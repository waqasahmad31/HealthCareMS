using HealthCareMS.Application.Portals;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Portals;

public sealed class PortalService(HealthCareDbContext dbContext) : IPortalService
{
    public async Task<Result<DoctorPortalDashboardResponse>> GetDoctorDashboardAsync(
        Guid doctorId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var doctor = await DoctorQuery()
            .SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);

        if (doctor is null)
        {
            return Result<DoctorPortalDashboardResponse>.Failure(new Error("PORTAL_DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        var todayAppointments = await DoctorAppointmentsForDate(doctorId, date)
            .OrderBy(x => x.ScheduledAt)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var upcoming = await AppointmentQuery()
            .Where(x =>
                x.DoctorId == doctorId
                && x.ScheduledAt >= now
                && x.Status != AppointmentStatus.Cancelled
                && x.Status != AppointmentStatus.Completed
                && x.Status != AppointmentStatus.NoShow)
            .OrderBy(x => x.ScheduledAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var response = new DoctorPortalDashboardResponse(
            doctor.Id,
            doctor.User.FullName,
            date,
            todayAppointments.Count,
            todayAppointments.Count(x => x.Status == AppointmentStatus.Completed),
            todayAppointments.Count(x => x.Type == AppointmentType.OnSite && x.CheckedInAt.HasValue && x.Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed),
            todayAppointments.Count(x => x.Status == AppointmentStatus.InProgress),
            upcoming.Count,
            todayAppointments.Select(x => x.PatientId).Distinct().Count(),
            todayAppointments.Where(x => x.Status == AppointmentStatus.Completed).Sum(x => x.ConsultationFee),
            doctor.Schedules.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).Select(MapSchedule).ToList(),
            todayAppointments.Select(MapSummary).ToList(),
            upcoming.Select(MapSummary).ToList());

        return Result<DoctorPortalDashboardResponse>.Success(response);
    }

    public async Task<Result<IReadOnlyList<PortalDoctorScheduleResponse>>> GetDoctorScheduleAsync(
        Guid doctorId,
        CancellationToken cancellationToken)
    {
        var doctorExists = await dbContext.Doctors.AnyAsync(x => x.Id == doctorId, cancellationToken);
        if (!doctorExists)
        {
            return Result<IReadOnlyList<PortalDoctorScheduleResponse>>.Failure(new Error("PORTAL_DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        var schedules = await dbContext.DoctorSchedules
            .Where(x => x.DoctorId == doctorId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PortalDoctorScheduleResponse>>.Success(schedules.Select(MapSchedule).ToList());
    }

    public async Task<Result<IReadOnlyList<PortalAppointmentSummaryResponse>>> GetDoctorAppointmentsAsync(
        Guid doctorId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var doctorExists = await dbContext.Doctors.AnyAsync(x => x.Id == doctorId, cancellationToken);
        if (!doctorExists)
        {
            return Result<IReadOnlyList<PortalAppointmentSummaryResponse>>.Failure(new Error("PORTAL_DOCTOR_NOT_FOUND", "Doctor was not found."));
        }

        var appointments = await DoctorAppointmentsForDate(doctorId, date)
            .OrderBy(x => x.ScheduledAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PortalAppointmentSummaryResponse>>.Success(appointments.Select(MapSummary).ToList());
    }

    public async Task<Result<PatientHistoryResponse>> GetPatientHistoryForDoctorAsync(
        Guid doctorId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var hasRelationship = await dbContext.Appointments.AnyAsync(
            x => x.DoctorId == doctorId && x.PatientId == patientId,
            cancellationToken);

        if (!hasRelationship)
        {
            return Result<PatientHistoryResponse>.Failure(new Error("PORTAL_PATIENT_HISTORY_NOT_FOUND", "Patient history was not found for this doctor."));
        }

        var patient = await dbContext.Patients
            .Include(x => x.User)
            .Include(x => x.MedicalHistory)
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        if (patient is null)
        {
            return Result<PatientHistoryResponse>.Failure(new Error("PORTAL_PATIENT_NOT_FOUND", "Patient was not found."));
        }

        var appointments = await AppointmentQuery()
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.ScheduledAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var completed = appointments.Where(x => x.Status == AppointmentStatus.Completed).ToList();
        var history = patient.MedicalHistory;
        var response = new PatientHistoryResponse(
            patient.Id,
            $"{patient.FirstName} {patient.LastName}".Trim(),
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.BloodGroup,
            history?.Allergies,
            history?.ChronicDiseases,
            history?.CurrentMedications,
            history?.PastSurgeries,
            history?.FamilyHistory,
            appointments.Count,
            completed.Count,
            completed.OrderByDescending(x => x.ScheduledAt).FirstOrDefault()?.ScheduledAt,
            appointments.Select(MapSummary).ToList());

        return Result<PatientHistoryResponse>.Success(response);
    }

    public async Task<Result<PatientPortalDashboardResponse>> GetPatientDashboardAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        if (patient is null)
        {
            return Result<PatientPortalDashboardResponse>.Failure(new Error("PORTAL_PATIENT_NOT_FOUND", "Patient was not found."));
        }

        var now = DateTimeOffset.UtcNow;
        var appointments = await AppointmentQuery()
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.ScheduledAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var upcoming = appointments
            .Where(x => x.ScheduledAt >= now && x.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.Completed and not AppointmentStatus.NoShow)
            .OrderBy(x => x.ScheduledAt)
            .Take(10)
            .ToList();

        var past = appointments
            .Where(x => x.ScheduledAt < now || x.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
            .OrderByDescending(x => x.ScheduledAt)
            .Take(10)
            .ToList();

        var quickActions = new List<string> { "Book Appointment", "View Appointment Detail" };
        if (upcoming.FirstOrDefault(x => x.Type == AppointmentType.OnSite)?.QueueNumber is not null)
        {
            quickActions.Add("Track Queue Status");
        }

        if (past.Any(x => x.Status == AppointmentStatus.Completed))
        {
            quickActions.Add("View Prescription");
        }

        var response = new PatientPortalDashboardResponse(
            patient.Id,
            $"{patient.FirstName} {patient.LastName}".Trim(),
            upcoming.Count,
            past.Count,
            upcoming.Select(MapSummary).FirstOrDefault(),
            upcoming.Select(MapSummary).ToList(),
            past.Select(MapSummary).ToList(),
            quickActions);

        return Result<PatientPortalDashboardResponse>.Success(response);
    }

    public async Task<Result<IReadOnlyList<PortalAppointmentSummaryResponse>>> GetPatientAppointmentsAsync(
        Guid patientId,
        string? status,
        CancellationToken cancellationToken)
    {
        var patientExists = await dbContext.Patients.AnyAsync(x => x.Id == patientId, cancellationToken);
        if (!patientExists)
        {
            return Result<IReadOnlyList<PortalAppointmentSummaryResponse>>.Failure(new Error("PORTAL_PATIENT_NOT_FOUND", "Patient was not found."));
        }

        var query = AppointmentQuery().Where(x => x.PatientId == patientId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        var appointments = await query
            .OrderByDescending(x => x.ScheduledAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PortalAppointmentSummaryResponse>>.Success(appointments.Select(MapSummary).ToList());
    }

    public async Task<Result<PortalAppointmentDetailResponse>> GetPatientAppointmentDetailAsync(
        Guid patientId,
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId && x.PatientId == patientId, cancellationToken);

        if (appointment is null)
        {
            return Result<PortalAppointmentDetailResponse>.Failure(new Error("PORTAL_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        var prescription = await dbContext.Prescriptions
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.AppointmentId == appointment.Id, cancellationToken);

        return Result<PortalAppointmentDetailResponse>.Success(MapDetail(appointment, prescription));
    }

    private IQueryable<Appointment> DoctorAppointmentsForDate(Guid doctorId, DateOnly date)
    {
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);

        return AppointmentQuery()
            .Where(x => x.DoctorId == doctorId && x.ScheduledAt >= start && x.ScheduledAt < end);
    }

    private IQueryable<Appointment> AppointmentQuery()
    {
        return dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User);
    }

    private IQueryable<Doctor> DoctorQuery()
    {
        return dbContext.Doctors
            .Include(x => x.User)
            .Include(x => x.Schedules);
    }

    private static PortalAppointmentSummaryResponse MapSummary(Appointment appointment)
    {
        return new PortalAppointmentSummaryResponse(
            appointment.Id,
            appointment.AppointmentNumber,
            appointment.PatientId,
            $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim(),
            appointment.DoctorId,
            appointment.Doctor.User.FullName,
            appointment.ScheduledAt,
            appointment.EndAt,
            appointment.Type.ToString(),
            appointment.Status.ToString(),
            appointment.Priority.ToString(),
            appointment.ReasonForVisit,
            appointment.QueueNumber,
            appointment.CheckedInAt,
            appointment.ConsultationFee);
    }

    private static PortalAppointmentDetailResponse MapDetail(Appointment appointment, Prescription? prescription)
    {
        return new PortalAppointmentDetailResponse(
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
            appointment.QueueNumber,
            appointment.CheckedInAt,
            appointment.ConsultationFee,
            prescription is null
                ? null
                : new PortalPrescriptionSummaryResponse(
                    prescription.Id,
                    prescription.PrescriptionNumber,
                    prescription.IssuedAt,
                    prescription.ValidUntil,
                    prescription.Status.ToString(),
                    prescription.Items.Count));
    }

    private static PortalDoctorScheduleResponse MapSchedule(DoctorSchedule schedule)
    {
        return new PortalDoctorScheduleResponse(
            schedule.Id,
            schedule.DayOfWeek.ToString(),
            schedule.StartTime,
            schedule.EndTime,
            schedule.SlotDurationMinutes,
            schedule.IsOnlineAvailable,
            schedule.IsOnSiteAvailable);
    }
}
