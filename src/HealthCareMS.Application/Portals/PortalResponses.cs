namespace HealthCareMS.Application.Portals;

public sealed record PortalAppointmentSummaryResponse(
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

public sealed record PortalPrescriptionSummaryResponse(
    Guid Id,
    string PrescriptionNumber,
    DateTimeOffset IssuedAt,
    DateTimeOffset ValidUntil,
    string Status,
    int ItemCount);

public sealed record PortalAppointmentDetailResponse(
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
    PortalPrescriptionSummaryResponse? Prescription);

public sealed record PortalDoctorScheduleResponse(
    Guid Id,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    short SlotDurationMinutes,
    bool IsOnlineAvailable,
    bool IsOnSiteAvailable);

public sealed record DoctorPortalDashboardResponse(
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
    IReadOnlyList<PortalDoctorScheduleResponse> MySchedule,
    IReadOnlyList<PortalAppointmentSummaryResponse> TodayAppointments,
    IReadOnlyList<PortalAppointmentSummaryResponse> UpcomingAppointments);

public sealed record PatientHistoryResponse(
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
    IReadOnlyList<PortalAppointmentSummaryResponse> RecentAppointments);

public sealed record PatientPortalDashboardResponse(
    Guid PatientId,
    string PatientName,
    int UpcomingCount,
    int PastCount,
    PortalAppointmentSummaryResponse? NextAppointment,
    IReadOnlyList<PortalAppointmentSummaryResponse> UpcomingAppointments,
    IReadOnlyList<PortalAppointmentSummaryResponse> PastAppointments,
    IReadOnlyList<string> QuickActions);

