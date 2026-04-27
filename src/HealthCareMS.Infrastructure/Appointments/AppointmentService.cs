using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Appointments;
using HealthCareMS.Application.Notifications;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Appointments;

public sealed class AppointmentService(
    HealthCareDbContext dbContext,
    ICurrentUser currentUser,
    INotificationService? notificationService = null) : IAppointmentService
{
    private static readonly short[] ValidDurations = [15, 20, 30, 45, 60];

    public async Task<Result<AppointmentResponse>> BookAsync(BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateBook(request);
        if (validationErrors.Count > 0)
        {
            return Result<AppointmentResponse>.Failure(Error.Validation(validationErrors));
        }

        var parseResult = ParseEnums(request.Type, request.Priority);
        if (parseResult.IsFailure)
        {
            return Result<AppointmentResponse>.Failure(parseResult.Error);
        }

        var patient = await dbContext.Patients
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == request.PatientId, cancellationToken);

        if (patient is null)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_PATIENT_NOT_FOUND", "Patient profile was not found."));
        }

        var doctor = await dbContext.Doctors
            .Include(x => x.User)
            .Include(x => x.Schedules)
            .SingleOrDefaultAsync(x => x.Id == request.DoctorId, cancellationToken);

        if (doctor is null || !doctor.IsActive || !doctor.IsVerified)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_DOCTOR_UNAVAILABLE", "Doctor is not available for booking."));
        }

        var requestedScheduledAt = request.ScheduledAt;
        var requestedEndAt = requestedScheduledAt.AddMinutes(request.DurationMinutes);
        var availability = ValidateAvailability(doctor.Schedules, requestedScheduledAt, requestedEndAt, parseResult.Value.Type);
        if (availability.IsFailure)
        {
            return Result<AppointmentResponse>.Failure(availability.Error);
        }

        var scheduledAt = requestedScheduledAt.ToUniversalTime();
        var endAt = requestedEndAt.ToUniversalTime();

        var conflictExists = await HasSlotConflictAsync(request.DoctorId, scheduledAt, endAt, excludedAppointmentId: null, cancellationToken);
        if (conflictExists)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_SLOT_CONFLICT", "Slot is already booked by another patient."));
        }

        var appointment = new Appointment
        {
            AppointmentNumber = await GenerateAppointmentNumberAsync(scheduledAt, cancellationToken),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            Patient = patient,
            Doctor = doctor,
            ScheduledAt = scheduledAt,
            EndAt = endAt,
            DurationMinutes = request.DurationMinutes,
            Type = parseResult.Value.Type,
            Status = AppointmentStatus.Pending,
            Priority = parseResult.Value.Priority,
            ReasonForVisit = request.ReasonForVisit.Trim(),
            PatientNotes = Normalize(request.PatientNotes),
            ConsultationFee = doctor.ConsultationFee,
            QueueNumber = parseResult.Value.Type == AppointmentType.OnSite
                ? await GenerateQueueNumberAsync(request.DoctorId, scheduledAt, cancellationToken)
                : null,
            CreatedByUserId = currentUser.UserId
        };

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (notificationService is not null)
        {
            await notificationService.NotifyAppointmentBookedAsync(appointment.Id, cancellationToken);
        }

        return Result<AppointmentResponse>.Success(Map(appointment));
    }

    public async Task<Result<AppointmentResponse>> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        return appointment is null
            ? Result<AppointmentResponse>.Failure(new Error("APT_NOT_FOUND", "Appointment was not found."))
            : Result<AppointmentResponse>.Success(Map(appointment));
    }

    public async Task<IReadOnlyList<AppointmentResponse>> SearchAsync(
        Guid? patientId,
        Guid? doctorId,
        string? status,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var query = AppointmentQuery();

        if (patientId.HasValue)
        {
            query = query.Where(x => x.PatientId == patientId.Value);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(x => x.DoctorId == doctorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (date.HasValue)
        {
            var start = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = start.AddDays(1);
            query = query.Where(x => x.ScheduledAt >= start && x.ScheduledAt < end);
        }

        var appointments = await query
            .OrderByDescending(x => x.ScheduledAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return appointments.Select(Map).ToList();
    }

    public async Task<Result<AppointmentResponse>> ConfirmAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_NOT_FOUND", "Appointment was not found."));
        }

        if (appointment.Status != AppointmentStatus.Pending)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_STATUS_INVALID", "Only pending appointments can be confirmed."));
        }

        appointment.Status = AppointmentStatus.Confirmed;
        if (appointment.Type == AppointmentType.Online)
        {
            appointment.MeetingLink ??= $"/consultation/waiting-room/{appointment.Id}";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AppointmentResponse>.Success(Map(appointment));
    }

    public async Task<Result<AppointmentResponse>> CancelAsync(
        Guid appointmentId,
        CancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CancellationReason))
        {
            return Result<AppointmentResponse>.Failure(Error.Validation([new ValidationError(nameof(request.CancellationReason), "CancellationReason is required.")]));
        }

        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_NOT_FOUND", "Appointment was not found."));
        }

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_STATUS_INVALID", "Completed or cancelled appointments cannot be cancelled."));
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = request.CancellationReason.Trim();
        appointment.CancelledBy = string.IsNullOrWhiteSpace(request.CancelledBy) ? "System" : request.CancelledBy.Trim();
        appointment.CancelledAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AppointmentResponse>.Success(Map(appointment));
    }

    public async Task<Result<AppointmentResponse>> RescheduleAsync(
        Guid appointmentId,
        RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidDurations.Contains(request.DurationMinutes))
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_DURATION_INVALID", "DurationMinutes must be one of 15, 20, 30, 45, 60."));
        }

        var appointment = await AppointmentQuery()
            .Include(x => x.Doctor.Schedules)
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_NOT_FOUND", "Appointment was not found."));
        }

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_STATUS_INVALID", "Completed or cancelled appointments cannot be rescheduled."));
        }

        var scheduleValidation = ValidateScheduleWindow(request.ScheduledAt, request.DurationMinutes);
        if (scheduleValidation.IsFailure)
        {
            return Result<AppointmentResponse>.Failure(scheduleValidation.Error);
        }

        var requestedScheduledAt = request.ScheduledAt;
        var requestedEndAt = requestedScheduledAt.AddMinutes(request.DurationMinutes);
        var availability = ValidateAvailability(appointment.Doctor.Schedules, requestedScheduledAt, requestedEndAt, appointment.Type);
        if (availability.IsFailure)
        {
            return Result<AppointmentResponse>.Failure(availability.Error);
        }

        var scheduledAt = requestedScheduledAt.ToUniversalTime();
        var endAt = requestedEndAt.ToUniversalTime();

        var conflictExists = await HasSlotConflictAsync(appointment.DoctorId, scheduledAt, endAt, appointment.Id, cancellationToken);
        if (conflictExists)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_SLOT_CONFLICT", "Slot is already booked by another patient."));
        }

        appointment.ScheduledAt = scheduledAt;
        appointment.EndAt = endAt;
        appointment.DurationMinutes = request.DurationMinutes;
        appointment.Status = AppointmentStatus.Pending;
        appointment.QueueNumber = appointment.Type == AppointmentType.OnSite
            ? await GenerateQueueNumberAsync(appointment.DoctorId, scheduledAt, cancellationToken)
            : null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AppointmentResponse>.Success(Map(appointment));
    }

    public async Task<Result<AppointmentResponse>> CompleteAsync(
        Guid appointmentId,
        CompleteAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(request.Diagnosis))
        {
            validationErrors.Add(new ValidationError(nameof(request.Diagnosis), "Diagnosis is required."));
        }

        if (validationErrors.Count > 0)
        {
            return Result<AppointmentResponse>.Failure(Error.Validation(validationErrors));
        }

        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_NOT_FOUND", "Appointment was not found."));
        }

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
        {
            return Result<AppointmentResponse>.Failure(new Error("APT_STATUS_INVALID", "Cancelled or no-show appointments cannot be completed."));
        }

        appointment.Status = AppointmentStatus.Completed;
        appointment.Diagnosis = request.Diagnosis.Trim();
        appointment.ClinicalNotes = Normalize(request.ClinicalNotes);
        appointment.FollowUpDate = request.FollowUpDate;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AppointmentResponse>.Success(Map(appointment));
    }

    private IQueryable<Appointment> AppointmentQuery()
    {
        return dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User);
    }

    private async Task<bool> HasSlotConflictAsync(
        Guid doctorId,
        DateTimeOffset scheduledAt,
        DateTimeOffset endAt,
        Guid? excludedAppointmentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Appointments.AnyAsync(x =>
            x.DoctorId == doctorId
            && x.Id != excludedAppointmentId
            && x.Status != AppointmentStatus.Cancelled
            && x.Status != AppointmentStatus.NoShow
            && scheduledAt < x.EndAt
            && endAt > x.ScheduledAt,
            cancellationToken);
    }

    private async Task<string> GenerateAppointmentNumberAsync(DateTimeOffset scheduledAt, CancellationToken cancellationToken)
    {
        var datePart = scheduledAt.UtcDateTime.ToString("yyyyMMdd");
        var prefix = $"APT-{datePart}-";
        var count = await dbContext.Appointments.CountAsync(x => x.AppointmentNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private async Task<int> GenerateQueueNumberAsync(Guid doctorId, DateTimeOffset scheduledAt, CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(scheduledAt.UtcDateTime.Date, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        var count = await dbContext.Appointments.CountAsync(x =>
            x.DoctorId == doctorId
            && x.Type == AppointmentType.OnSite
            && x.ScheduledAt >= dayStart
            && x.ScheduledAt < dayEnd
            && x.Status != AppointmentStatus.Cancelled,
            cancellationToken);

        return count + 1;
    }

    private static Result<(AppointmentType Type, AppointmentPriority Priority)> ParseEnums(string type, string? priority)
    {
        if (!Enum.TryParse<AppointmentType>(type, ignoreCase: true, out var appointmentType))
        {
            return Result<(AppointmentType, AppointmentPriority)>.Failure(new Error("APT_TYPE_INVALID", "Type must be OnSite or Online."));
        }

        var appointmentPriority = AppointmentPriority.Normal;
        if (!string.IsNullOrWhiteSpace(priority)
            && !Enum.TryParse(priority, ignoreCase: true, out appointmentPriority))
        {
            return Result<(AppointmentType, AppointmentPriority)>.Failure(new Error("APT_PRIORITY_INVALID", "Priority must be Low, Normal, High, or Urgent."));
        }

        return Result<(AppointmentType, AppointmentPriority)>.Success((appointmentType, appointmentPriority));
    }

    private static List<ValidationError> ValidateBook(BookAppointmentRequest request)
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

        if (!ValidDurations.Contains(request.DurationMinutes))
        {
            errors.Add(new ValidationError(nameof(request.DurationMinutes), "DurationMinutes must be one of 15, 20, 30, 45, 60."));
        }

        var scheduleValidation = ValidateScheduleWindow(request.ScheduledAt, request.DurationMinutes);
        if (scheduleValidation.IsFailure)
        {
            errors.Add(new ValidationError(nameof(request.ScheduledAt), scheduleValidation.Error.Message));
        }

        return errors;
    }

    private static Result ValidateScheduleWindow(DateTimeOffset scheduledAt, short durationMinutes)
    {
        var now = DateTimeOffset.UtcNow;
        if (scheduledAt <= now)
        {
            return Result.Failure(new Error("APT_PAST_TIME", "ScheduledAt must be in the future."));
        }

        if (scheduledAt < now.AddMinutes(30))
        {
            return Result.Failure(new Error("APT_TOO_SOON", "ScheduledAt must be at least 30 minutes from now."));
        }

        if (scheduledAt > now.AddMonths(3))
        {
            return Result.Failure(new Error("APT_MAX_ADVANCE", "ScheduledAt cannot be more than 3 months in future."));
        }

        if (durationMinutes <= 0)
        {
            return Result.Failure(new Error("APT_DURATION_INVALID", "DurationMinutes must be positive."));
        }

        return Result.Success();
    }

    private static Result ValidateAvailability(
        IEnumerable<DoctorSchedule> schedules,
        DateTimeOffset scheduledAt,
        DateTimeOffset endAt,
        AppointmentType appointmentType)
    {
        var startTime = TimeOnly.FromDateTime(scheduledAt.DateTime);
        var endTime = TimeOnly.FromDateTime(endAt.DateTime);
        var dayOfWeek = scheduledAt.DateTime.DayOfWeek;

        var available = schedules.Any(x =>
            x.DayOfWeek == dayOfWeek
            && startTime >= x.StartTime
            && endTime <= x.EndTime
            && (appointmentType == AppointmentType.Online ? x.IsOnlineAvailable : x.IsOnSiteAvailable));

        return available
            ? Result.Success()
            : Result.Failure(new Error("APT_DOCTOR_UNAVAILABLE", "Doctor is not available on selected day/time."));
    }

    private static AppointmentResponse Map(Appointment appointment)
    {
        return new AppointmentResponse(
            appointment.Id,
            appointment.AppointmentNumber,
            appointment.PatientId,
            $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim(),
            appointment.DoctorId,
            appointment.Doctor.User.FullName,
            appointment.ScheduledAt,
            appointment.EndAt,
            appointment.DurationMinutes,
            appointment.Type.ToString(),
            appointment.Status.ToString(),
            appointment.Priority.ToString(),
            appointment.ReasonForVisit,
            appointment.PatientNotes,
            appointment.Diagnosis,
            appointment.Icd10Code,
            appointment.Icd10Title,
            appointment.ClinicalNotes,
            appointment.FollowUpDate,
            appointment.CancellationReason,
            appointment.CancelledBy,
            appointment.CancelledAt,
            appointment.ConsultationFee,
            appointment.PaymentStatus.ToString(),
            appointment.MeetingLink,
            appointment.QueueNumber,
            appointment.CheckedInAt,
            appointment.CreatedAt);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
