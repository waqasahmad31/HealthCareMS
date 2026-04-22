using System.Text.Json;
using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Application.Patients;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Patients;

public sealed class PatientService(
    HealthCareDbContext dbContext,
    IPasswordHasher passwordHasher) : IPatientService
{
    public async Task<Result<PatientResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRegistration(request);
        if (validationErrors.Count > 0)
        {
            return Result<PatientResponse>.Failure(Error.Validation(validationErrors));
        }

        if (!Enum.TryParse<Gender>(request.Gender, ignoreCase: true, out var gender))
        {
            return Result<PatientResponse>.Failure(new Error("PATIENT_GENDER_INVALID", "Gender must be Male, Female, or Other."));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (emailExists)
        {
            return Result<PatientResponse>.Failure(new Error("PATIENT_EMAIL_EXISTS", "A user with this email already exists."));
        }

        if (!string.IsNullOrWhiteSpace(request.Cnic))
        {
            var cnic = request.Cnic.Trim();
            var cnicExists = await dbContext.Patients.AnyAsync(x => x.Cnic == cnic, cancellationToken);
            if (cnicExists)
            {
                return Result<PatientResponse>.Failure(new Error("PATIENT_CNIC_EXISTS", "A patient with this CNIC already exists."));
            }
        }

        var patientRole = await dbContext.Roles
            .SingleOrDefaultAsync(x => x.TenantId == null && x.Name == "Patient", cancellationToken);

        if (patientRole is null)
        {
            return Result<PatientResponse>.Failure(new Error("PATIENT_ROLE_MISSING", "Patient role is not seeded."));
        }

        var user = new ApplicationUser
        {
            RoleId = patientRole.Id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = Normalize(request.Phone),
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true,
            IsEmailVerified = true
        };

        var patient = new Patient
        {
            User = user,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Cnic = Normalize(request.Cnic),
            DateOfBirth = request.DateOfBirth,
            Gender = gender,
            BloodGroup = Normalize(request.BloodGroup),
            Phone = Normalize(request.Phone),
            AlternatePhone = Normalize(request.AlternatePhone),
            AddressStreet = Normalize(request.AddressStreet),
            AddressCity = Normalize(request.AddressCity),
            AddressProvince = Normalize(request.AddressProvince),
            AddressPostalCode = Normalize(request.AddressPostalCode),
            EmergencyContactName = Normalize(request.EmergencyContactName),
            EmergencyContactPhone = Normalize(request.EmergencyContactPhone),
            EmergencyContactRelation = Normalize(request.EmergencyContactRelation),
            MedicalHistory = new MedicalHistory()
        };

        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PatientResponse>.Success(Map(patient));
    }

    public async Task<Result<PatientResponse>> GetByIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await PatientQuery()
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        return patient is null
            ? Result<PatientResponse>.Failure(new Error("PATIENT_NOT_FOUND", "Patient was not found."))
            : Result<PatientResponse>.Success(Map(patient));
    }

    public async Task<IReadOnlyList<PatientResponse>> SearchAsync(string? search, CancellationToken cancellationToken)
    {
        var query = PatientQuery();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(term)
                || x.LastName.ToLower().Contains(term)
                || x.User.Email.ToLower().Contains(term)
                || (x.Phone != null && x.Phone.Contains(term))
                || (x.Cnic != null && x.Cnic.Contains(term)));
        }

        var patients = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return patients.Select(Map).ToList();
    }

    public async Task<Result<PatientResponse>> UpdateProfileAsync(
        Guid patientId,
        UpdatePatientProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateProfile(request);
        if (validationErrors.Count > 0)
        {
            return Result<PatientResponse>.Failure(Error.Validation(validationErrors));
        }

        var patient = await PatientQuery()
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        if (patient is null)
        {
            return Result<PatientResponse>.Failure(new Error("PATIENT_NOT_FOUND", "Patient was not found."));
        }

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.BloodGroup = Normalize(request.BloodGroup);
        patient.Phone = Normalize(request.Phone);
        patient.AlternatePhone = Normalize(request.AlternatePhone);
        patient.AddressStreet = Normalize(request.AddressStreet);
        patient.AddressCity = Normalize(request.AddressCity);
        patient.AddressProvince = Normalize(request.AddressProvince);
        patient.AddressPostalCode = Normalize(request.AddressPostalCode);
        patient.EmergencyContactName = Normalize(request.EmergencyContactName);
        patient.EmergencyContactPhone = Normalize(request.EmergencyContactPhone);
        patient.EmergencyContactRelation = Normalize(request.EmergencyContactRelation);
        patient.InsuranceProvider = Normalize(request.InsuranceProvider);
        patient.InsurancePolicyNo = Normalize(request.InsurancePolicyNo);
        patient.User.FirstName = patient.FirstName;
        patient.User.LastName = patient.LastName;
        patient.User.PhoneNumber = patient.Phone;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PatientResponse>.Success(Map(patient));
    }

    public async Task<Result<MedicalHistoryResponse>> UpdateMedicalHistoryAsync(
        Guid patientId,
        UpdateMedicalHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateMedicalHistory(request);
        if (validationErrors.Count > 0)
        {
            return Result<MedicalHistoryResponse>.Failure(Error.Validation(validationErrors));
        }

        var patient = await dbContext.Patients
            .Include(x => x.MedicalHistory)
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        if (patient is null)
        {
            return Result<MedicalHistoryResponse>.Failure(new Error("PATIENT_NOT_FOUND", "Patient was not found."));
        }

        patient.MedicalHistory ??= new MedicalHistory { PatientId = patient.Id };
        patient.MedicalHistory.Allergies = request.Allergies;
        patient.MedicalHistory.ChronicDiseases = request.ChronicDiseases;
        patient.MedicalHistory.CurrentMedications = request.CurrentMedications;
        patient.MedicalHistory.PastSurgeries = request.PastSurgeries;
        patient.MedicalHistory.FamilyHistory = request.FamilyHistory;
        patient.MedicalHistory.SmokingStatus = request.SmokingStatus.Trim();
        patient.MedicalHistory.AlcoholStatus = request.AlcoholStatus.Trim();
        patient.MedicalHistory.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<MedicalHistoryResponse>.Success(Map(patient.MedicalHistory));
    }

    private IQueryable<Patient> PatientQuery()
    {
        return dbContext.Patients
            .Include(x => x.User)
            .Include(x => x.MedicalHistory);
    }

    private static PatientResponse Map(Patient patient)
    {
        return new PatientResponse(
            patient.Id,
            patient.UserId,
            patient.FirstName,
            patient.LastName,
            patient.User.Email,
            patient.Cnic,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.BloodGroup,
            patient.Phone,
            patient.AlternatePhone,
            patient.AddressStreet,
            patient.AddressCity,
            patient.AddressProvince,
            patient.AddressPostalCode,
            patient.EmergencyContactName,
            patient.EmergencyContactPhone,
            patient.EmergencyContactRelation,
            patient.InsuranceProvider,
            patient.InsurancePolicyNo,
            patient.IsActive,
            patient.CreatedAt,
            patient.MedicalHistory is null ? null : Map(patient.MedicalHistory));
    }

    private static MedicalHistoryResponse Map(MedicalHistory medicalHistory)
    {
        return new MedicalHistoryResponse(
            medicalHistory.Id,
            medicalHistory.PatientId,
            medicalHistory.Allergies,
            medicalHistory.ChronicDiseases,
            medicalHistory.CurrentMedications,
            medicalHistory.PastSurgeries,
            medicalHistory.FamilyHistory,
            medicalHistory.SmokingStatus,
            medicalHistory.AlcoholStatus,
            medicalHistory.UpdatedAt);
    }

    private static List<ValidationError> ValidateRegistration(RegisterPatientRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.FirstName, nameof(request.FirstName), errors);
        Required(request.LastName, nameof(request.LastName), errors);
        Required(request.Email, nameof(request.Email), errors);
        Required(request.Password, nameof(request.Password), errors);
        Required(request.Gender, nameof(request.Gender), errors);

        if (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 8)
        {
            errors.Add(new ValidationError(nameof(request.Password), "Password must be at least 8 characters."));
        }

        if (request.DateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            errors.Add(new ValidationError(nameof(request.DateOfBirth), "DateOfBirth must be in the past."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateProfile(UpdatePatientProfileRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.FirstName, nameof(request.FirstName), errors);
        Required(request.LastName, nameof(request.LastName), errors);
        return errors;
    }

    private static List<ValidationError> ValidateMedicalHistory(UpdateMedicalHistoryRequest request)
    {
        var errors = new List<ValidationError>();
        ValidateJsonArray(request.Allergies, nameof(request.Allergies), errors);
        ValidateJsonArray(request.ChronicDiseases, nameof(request.ChronicDiseases), errors);
        ValidateJsonArray(request.CurrentMedications, nameof(request.CurrentMedications), errors);
        ValidateJsonArray(request.PastSurgeries, nameof(request.PastSurgeries), errors);
        ValidateJsonArray(request.FamilyHistory, nameof(request.FamilyHistory), errors);
        Required(request.SmokingStatus, nameof(request.SmokingStatus), errors);
        Required(request.AlcoholStatus, nameof(request.AlcoholStatus), errors);
        return errors;
    }

    private static void ValidateJsonArray(string? value, string field, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(field, $"{field} is required."));
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add(new ValidationError(field, $"{field} must be a JSON array."));
            }
        }
        catch (JsonException)
        {
            errors.Add(new ValidationError(field, $"{field} must be valid JSON."));
        }
    }

    private static void Required(string? value, string field, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(field, $"{field} is required."));
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
