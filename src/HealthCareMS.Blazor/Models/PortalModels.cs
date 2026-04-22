namespace HealthCareMS.Blazor.Models;

public sealed record PortalAppointmentSummaryModel(
    Guid Id,
    string AppointmentNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateTimeOffset ScheduledAt,
    DateTimeOffset EndAt,
    string Type,
    string Status,
    string Priority,
    string? ReasonForVisit,
    int? QueueNumber,
    DateTimeOffset? CheckedInAt,
    decimal ConsultationFee);

public sealed record PortalPrescriptionSummaryModel(
    Guid Id,
    string PrescriptionNumber,
    DateTimeOffset IssuedAt,
    DateTimeOffset ValidUntil,
    string Status,
    int ItemCount);

public sealed record PortalAppointmentDetailModel(
    Guid Id,
    string AppointmentNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateTimeOffset ScheduledAt,
    DateTimeOffset EndAt,
    short DurationMinutes,
    string Type,
    string Status,
    string Priority,
    string? ReasonForVisit,
    string? PatientNotes,
    string? Diagnosis,
    string? Icd10Code,
    string? Icd10Title,
    string? ClinicalNotes,
    DateOnly? FollowUpDate,
    int? QueueNumber,
    DateTimeOffset? CheckedInAt,
    decimal ConsultationFee,
    PortalPrescriptionSummaryModel? Prescription);

public sealed record PortalDoctorScheduleModel(
    Guid Id,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    short SlotDurationMinutes,
    bool IsOnlineAvailable,
    bool IsOnSiteAvailable);

public sealed record DoctorPortalDashboardModel(
    Guid DoctorId,
    string DoctorName,
    DateOnly Date,
    int TodayAppointmentCount,
    int TodayCompletedCount,
    int WaitingQueueCount,
    int InProgressCount,
    int UpcomingAppointmentCount,
    int UniquePatientCount,
    decimal TodayConsultationFeeTotal,
    IReadOnlyList<PortalDoctorScheduleModel> MySchedule,
    IReadOnlyList<PortalAppointmentSummaryModel> TodayAppointments,
    IReadOnlyList<PortalAppointmentSummaryModel> UpcomingAppointments);

public sealed record PatientHistoryModel(
    Guid PatientId,
    string PatientName,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodGroup,
    string? Allergies,
    string? ChronicDiseases,
    string? CurrentMedications,
    string? PastSurgeries,
    string? FamilyHistory,
    int TotalAppointments,
    int CompletedConsultations,
    DateTimeOffset? LastVisitAt,
    IReadOnlyList<PortalAppointmentSummaryModel> RecentAppointments);

public sealed record PatientPortalDashboardModel(
    Guid PatientId,
    string PatientName,
    int UpcomingCount,
    int PastCount,
    PortalAppointmentSummaryModel? NextAppointment,
    IReadOnlyList<PortalAppointmentSummaryModel> UpcomingAppointments,
    IReadOnlyList<PortalAppointmentSummaryModel> PastAppointments,
    IReadOnlyList<string> QuickActions);
