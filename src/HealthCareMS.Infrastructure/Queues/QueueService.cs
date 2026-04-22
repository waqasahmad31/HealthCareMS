using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Queues;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Queues;

public sealed class QueueService(
    HealthCareDbContext dbContext,
    ICurrentUser currentUser) : IQueueService
{
    private const short DefaultWalkInDurationMinutes = 30;

    public async Task<Result<QueueEntryResponse>> RegisterWalkInAsync(
        WalkInRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateWalkIn(request);
        if (validationErrors.Count > 0)
        {
            return Result<QueueEntryResponse>.Failure(Error.Validation(validationErrors));
        }

        if (!Enum.TryParse<AppointmentPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            priority = AppointmentPriority.Normal;
        }

        var patient = await dbContext.Patients
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == request.PatientId, cancellationToken);

        if (patient is null)
        {
            return Result<QueueEntryResponse>.Failure(new Error("QUEUE_PATIENT_NOT_FOUND", "Patient profile was not found."));
        }

        var doctor = await dbContext.Doctors
            .Include(x => x.User)
            .Include(x => x.Schedules)
            .SingleOrDefaultAsync(x => x.Id == request.DoctorId, cancellationToken);

        if (doctor is null || !doctor.IsActive || !doctor.IsVerified)
        {
            return Result<QueueEntryResponse>.Failure(new Error("QUEUE_DOCTOR_UNAVAILABLE", "Doctor is not available for queue registration."));
        }

        var now = DateTimeOffset.UtcNow;
        var scheduleResult = ResolveWalkInSchedule(doctor.Schedules, now);
        if (scheduleResult.IsFailure)
        {
            return Result<QueueEntryResponse>.Failure(scheduleResult.Error);
        }

        var date = DateOnly.FromDateTime(scheduleResult.Value.ScheduledAt.UtcDateTime);
        var appointment = new Appointment
        {
            AppointmentNumber = await GenerateAppointmentNumberAsync(scheduleResult.Value.ScheduledAt, cancellationToken),
            PatientId = patient.Id,
            Patient = patient,
            DoctorId = doctor.Id,
            Doctor = doctor,
            ScheduledAt = scheduleResult.Value.ScheduledAt,
            EndAt = scheduleResult.Value.ScheduledAt.AddMinutes(scheduleResult.Value.DurationMinutes),
            DurationMinutes = scheduleResult.Value.DurationMinutes,
            Type = AppointmentType.OnSite,
            Status = AppointmentStatus.Confirmed,
            Priority = priority,
            ReasonForVisit = request.ReasonForVisit.Trim(),
            PatientNotes = Normalize(request.PatientNotes),
            ConsultationFee = doctor.ConsultationFee,
            QueueNumber = await GenerateQueueNumberAsync(doctor.Id, date, cancellationToken),
            CheckedInAt = now,
            CreatedByUserId = currentUser.UserId
        };

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var board = await BuildBoardAsync(doctor.Id, date, cancellationToken);
        var queueEntry = board.Entries.Single(x => x.AppointmentId == appointment.Id);
        return Result<QueueEntryResponse>.Success(queueEntry);
    }

    public async Task<Result<QueueEntryResponse>> CheckInAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<QueueEntryResponse>.Failure(new Error("QUEUE_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        if (appointment.Type != AppointmentType.OnSite)
        {
            return Result<QueueEntryResponse>.Failure(new Error("QUEUE_TYPE_INVALID", "Only onsite appointments can be checked in."));
        }

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
        {
            return Result<QueueEntryResponse>.Failure(new Error("QUEUE_STATUS_INVALID", "Completed, cancelled, or no-show appointments cannot be checked in."));
        }

        var date = DateOnly.FromDateTime(appointment.ScheduledAt.UtcDateTime);
        appointment.CheckedInAt ??= DateTimeOffset.UtcNow;
        appointment.QueueNumber ??= await GenerateQueueNumberAsync(appointment.DoctorId, date, cancellationToken);
        if (appointment.Status == AppointmentStatus.Pending)
        {
            appointment.Status = AppointmentStatus.Confirmed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var board = await BuildBoardAsync(appointment.DoctorId, date, cancellationToken);
        var queueEntry = board.Entries.Single(x => x.AppointmentId == appointment.Id);
        return Result<QueueEntryResponse>.Success(queueEntry);
    }

    public async Task<Result<QueueEntryResponse>> CallNextAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken)
    {
        if (doctorId == Guid.Empty)
        {
            return Result<QueueEntryResponse>.Failure(new Error("QUEUE_DOCTOR_INVALID", "DoctorId is required."));
        }

        var activeQueue = await GetQueueAppointmentsAsync(doctorId, date, cancellationToken);
        var current = activeQueue.FirstOrDefault(x => x.Status == AppointmentStatus.InProgress);
        if (current is not null)
        {
            return Result<QueueEntryResponse>.Success(MapEntry(current, position: 0, estimatedWaitMinutes: 0));
        }

        var next = activeQueue
            .Where(IsWaiting)
            .OrderBy(x => x.QueueNumber ?? int.MaxValue)
            .ThenBy(x => x.CheckedInAt)
            .ThenBy(x => x.ScheduledAt)
            .FirstOrDefault();

        if (next is null)
        {
            return Result<QueueEntryResponse>.Failure(new Error("QUEUE_EMPTY", "No checked-in patients are waiting in the queue."));
        }

        next.Status = AppointmentStatus.InProgress;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<QueueEntryResponse>.Success(MapEntry(next, position: 0, estimatedWaitMinutes: 0));
    }

    public async Task<Result<QueueEntryResponse>> GetNextPatientAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken)
    {
        var boardResult = await GetBoardAsync(doctorId, date, cancellationToken);
        if (boardResult.IsFailure)
        {
            return Result<QueueEntryResponse>.Failure(boardResult.Error);
        }

        return boardResult.Value.NextPatient is null
            ? Result<QueueEntryResponse>.Failure(new Error("QUEUE_EMPTY", "No checked-in patients are waiting in the queue."))
            : Result<QueueEntryResponse>.Success(boardResult.Value.NextPatient);
    }

    public async Task<Result<QueueBoardResponse>> GetBoardAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken)
    {
        if (doctorId == Guid.Empty)
        {
            return Result<QueueBoardResponse>.Failure(new Error("QUEUE_DOCTOR_INVALID", "DoctorId is required."));
        }

        var doctorExists = await dbContext.Doctors.AnyAsync(x => x.Id == doctorId, cancellationToken);
        if (!doctorExists)
        {
            return Result<QueueBoardResponse>.Failure(new Error("QUEUE_DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        return Result<QueueBoardResponse>.Success(await BuildBoardAsync(doctorId, date, cancellationToken));
    }

    public async Task<Result<PatientQueueStatusResponse>> GetPatientStatusAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<PatientQueueStatusResponse>.Failure(new Error("QUEUE_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        var date = DateOnly.FromDateTime(appointment.ScheduledAt.UtcDateTime);
        var board = await BuildBoardAsync(appointment.DoctorId, date, cancellationToken);
        var entry = board.Entries.SingleOrDefault(x => x.AppointmentId == appointment.Id);

        return Result<PatientQueueStatusResponse>.Success(new PatientQueueStatusResponse(
            appointment.Id,
            appointment.AppointmentNumber,
            appointment.DoctorId,
            appointment.Doctor.User.FullName,
            date,
            appointment.QueueNumber,
            appointment.Status.ToString(),
            entry?.Position ?? 0,
            entry?.EstimatedWaitMinutes ?? 0,
            appointment.CheckedInAt,
            DateTimeOffset.UtcNow));
    }

    private async Task<QueueBoardResponse> BuildBoardAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken)
    {
        var appointments = await GetQueueAppointmentsAsync(doctorId, date, cancellationToken);
        var doctorName = appointments.FirstOrDefault()?.Doctor.User.FullName
            ?? await dbContext.Doctors
                .Where(x => x.Id == doctorId)
                .Select(x => x.User.FullName)
                .SingleAsync(cancellationToken);

        var averageSlotMinutes = appointments.Count > 0
            ? Math.Max(1, (int)Math.Round(appointments.Average(x => x.DurationMinutes)))
            : DefaultWalkInDurationMinutes;

        var waiting = appointments
            .Where(IsWaiting)
            .OrderBy(x => x.QueueNumber ?? int.MaxValue)
            .ThenBy(x => x.CheckedInAt)
            .ThenBy(x => x.ScheduledAt)
            .ToList();

        var inProgress = appointments
            .Where(x => x.Status == AppointmentStatus.InProgress)
            .OrderBy(x => x.QueueNumber ?? int.MaxValue)
            .ThenBy(x => x.ScheduledAt)
            .ToList();

        var entries = inProgress
            .Select(x => MapEntry(x, position: 0, estimatedWaitMinutes: 0))
            .Concat(waiting.Select((x, index) =>
            {
                var position = index + 1;
                var estimatedWait = (inProgress.Count > 0 ? position : position - 1) * averageSlotMinutes;
                return MapEntry(x, position, estimatedWait);
            }))
            .ToList();

        return new QueueBoardResponse(
            doctorId,
            doctorName,
            date,
            waiting.Count,
            inProgress.Count,
            appointments.Count(x => x.Status == AppointmentStatus.Completed),
            averageSlotMinutes,
            entries.FirstOrDefault(),
            entries,
            DateTimeOffset.UtcNow);
    }

    private async Task<List<Appointment>> GetQueueAppointmentsAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        return await AppointmentQuery()
            .Where(x =>
                x.DoctorId == doctorId
                && x.Type == AppointmentType.OnSite
                && x.ScheduledAt >= dayStart
                && x.ScheduledAt < dayEnd
                && x.Status != AppointmentStatus.Cancelled
                && x.Status != AppointmentStatus.NoShow)
            .OrderBy(x => x.QueueNumber ?? int.MaxValue)
            .ThenBy(x => x.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Appointment> AppointmentQuery()
    {
        return dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User);
    }

    private async Task<string> GenerateAppointmentNumberAsync(DateTimeOffset scheduledAt, CancellationToken cancellationToken)
    {
        var datePart = scheduledAt.UtcDateTime.ToString("yyyyMMdd");
        var prefix = $"APT-{datePart}-";
        var count = await dbContext.Appointments.CountAsync(x => x.AppointmentNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private async Task<int> GenerateQueueNumberAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        var maxQueueNumber = await dbContext.Appointments
            .Where(x =>
                x.DoctorId == doctorId
                && x.Type == AppointmentType.OnSite
                && x.ScheduledAt >= dayStart
                && x.ScheduledAt < dayEnd
                && x.Status != AppointmentStatus.Cancelled
                && x.QueueNumber.HasValue)
            .MaxAsync(x => x.QueueNumber, cancellationToken);

        return (maxQueueNumber ?? 0) + 1;
    }

    private static Result<(DateTimeOffset ScheduledAt, short DurationMinutes)> ResolveWalkInSchedule(
        IEnumerable<DoctorSchedule> schedules,
        DateTimeOffset now)
    {
        var date = DateOnly.FromDateTime(now.UtcDateTime);
        var today = now.UtcDateTime.DayOfWeek;
        var scheduleWindows = schedules
            .Where(x => x.DayOfWeek == today && x.IsOnSiteAvailable)
            .Select(x =>
            {
                var startAt = new DateTimeOffset(date.ToDateTime(x.StartTime), TimeSpan.Zero);
                var endAt = new DateTimeOffset(date.ToDateTime(x.EndTime), TimeSpan.Zero);
                var duration = x.SlotDurationMinutes > 0 ? x.SlotDurationMinutes : DefaultWalkInDurationMinutes;
                return new { StartAt = startAt, EndAt = endAt, Duration = duration };
            })
            .Where(x => x.EndAt > now && x.StartAt.AddMinutes(x.Duration) <= x.EndAt)
            .OrderBy(x => x.StartAt)
            .ToList();

        var activeWindow = scheduleWindows.FirstOrDefault(x =>
            now >= x.StartAt && now.AddMinutes(x.Duration) <= x.EndAt);
        if (activeWindow is not null)
        {
            return Result<(DateTimeOffset, short)>.Success((now, activeWindow.Duration));
        }

        var upcomingWindow = scheduleWindows.FirstOrDefault(x => now < x.StartAt);
        if (upcomingWindow is not null)
        {
            return Result<(DateTimeOffset, short)>.Success((upcomingWindow.StartAt, upcomingWindow.Duration));
        }

        return Result<(DateTimeOffset, short)>.Failure(new Error("QUEUE_DOCTOR_UNAVAILABLE", "Doctor has no onsite queue window available today."));
    }

    private static List<ValidationError> ValidateWalkIn(WalkInRegistrationRequest request)
    {
        var errors = new List<ValidationError>();
        if (request.PatientId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(request.PatientId), "PatientId is required."));
        }

        if (request.DoctorId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(request.DoctorId), "DoctorId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ReasonForVisit) || request.ReasonForVisit.Trim().Length < 10)
        {
            errors.Add(new ValidationError(nameof(request.ReasonForVisit), "ReasonForVisit must be at least 10 characters."));
        }

        if (!string.IsNullOrWhiteSpace(request.Priority)
            && !Enum.TryParse<AppointmentPriority>(request.Priority, ignoreCase: true, out _))
        {
            errors.Add(new ValidationError(nameof(request.Priority), "Priority must be Low, Normal, High, or Urgent."));
        }

        return errors;
    }

    private static bool IsWaiting(Appointment appointment)
    {
        return appointment.CheckedInAt.HasValue
            && appointment.Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;
    }

    private static QueueEntryResponse MapEntry(Appointment appointment, int position, int estimatedWaitMinutes)
    {
        return new QueueEntryResponse(
            appointment.Id,
            appointment.AppointmentNumber,
            appointment.PatientId,
            $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim(),
            appointment.DoctorId,
            appointment.Doctor.User.FullName,
            appointment.QueueNumber ?? 0,
            appointment.CheckedInAt,
            appointment.ScheduledAt,
            appointment.Status.ToString(),
            appointment.Priority.ToString(),
            appointment.ReasonForVisit ?? string.Empty,
            position,
            estimatedWaitMinutes);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
