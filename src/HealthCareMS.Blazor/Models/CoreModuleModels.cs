namespace HealthCareMS.Blazor.Models;

public sealed record PatientModel(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? Cnic,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodGroup,
    string? Phone,
    string? AlternatePhone,
    string? AddressStreet,
    string? AddressCity,
    string? AddressProvince,
    string? AddressPostalCode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelation,
    string? InsuranceProvider,
    string? InsurancePolicyNo,
    bool IsActive,
    DateTimeOffset CreatedAt,
    MedicalHistoryModel? MedicalHistory);

public sealed record MedicalHistoryModel(
    Guid Id,
    Guid PatientId,
    string Allergies,
    string ChronicDiseases,
    string CurrentMedications,
    string PastSurgeries,
    string FamilyHistory,
    string SmokingStatus,
    string AlcoholStatus,
    DateTimeOffset? UpdatedAt);

public sealed class RegisterPatientModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = "ChangeMe@12345";
    public string? Cnic { get; set; }
    public DateOnly DateOfBirth { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-25));
    public string Gender { get; set; } = "Female";
    public string? BloodGroup { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressProvince { get; set; }
    public string? AddressPostalCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
}

public sealed class UpdatePatientProfileModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? BloodGroup { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressProvince { get; set; }
    public string? AddressPostalCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNo { get; set; }
}

public sealed class UpdateMedicalHistoryModel
{
    public string Allergies { get; set; } = string.Empty;
    public string ChronicDiseases { get; set; } = string.Empty;
    public string CurrentMedications { get; set; } = string.Empty;
    public string PastSurgeries { get; set; } = string.Empty;
    public string FamilyHistory { get; set; } = string.Empty;
    public string SmokingStatus { get; set; } = "Never";
    public string AlcoholStatus { get; set; } = "Never";
}

public sealed record AvailableSlotModel(TimeOnly StartTime, TimeOnly EndTime, string AppointmentType);

public sealed class UpdateDoctorProfileModel
{
    public string Specialization { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public string? Biography { get; set; }
    public string City { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VerifyDoctorModel
{
    public bool IsVerified { get; set; }
}

public sealed class SetDoctorScheduleModel
{
    public IReadOnlyList<DoctorScheduleSlotModel> Slots { get; set; } = [];
}

public sealed class DoctorScheduleSlotModel
{
    public string DayOfWeek { get; set; } = "Monday";
    public TimeOnly StartTime { get; set; } = new(9, 0);
    public TimeOnly EndTime { get; set; } = new(13, 0);
    public short SlotDurationMinutes { get; set; } = 30;
    public bool IsOnlineAvailable { get; set; } = true;
    public bool IsOnSiteAvailable { get; set; } = true;
}

public sealed record AppointmentModel(
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
    string? CancellationReason,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    decimal ConsultationFee,
    string PaymentStatus,
    string? MeetingLink,
    int? QueueNumber,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset CreatedAt);

public sealed class BookAppointmentModel
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; } = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(9);
    public string Type { get; set; } = "OnSite";
    public short DurationMinutes { get; set; } = 30;
    public string ReasonForVisit { get; set; } = string.Empty;
    public string? Priority { get; set; } = "Normal";
    public string? PatientNotes { get; set; }
}

public sealed class CancelAppointmentModel
{
    public string CancellationReason { get; set; } = string.Empty;
    public string CancelledBy { get; set; } = "Front Desk";
}

public sealed class RescheduleAppointmentModel
{
    public DateTimeOffset ScheduledAt { get; set; } = DateTimeOffset.UtcNow.AddDays(2).Date.AddHours(11);
    public short DurationMinutes { get; set; } = 30;
    public string? Reason { get; set; }
}

public sealed class CompleteAppointmentModel
{
    public string Diagnosis { get; set; } = string.Empty;
    public string? ClinicalNotes { get; set; }
    public DateOnly? FollowUpDate { get; set; }
}
