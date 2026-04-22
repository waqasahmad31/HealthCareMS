using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Consultations;

public sealed class ConsultationService(HealthCareDbContext dbContext) : IConsultationService
{
    private static readonly Icd10CodeResponse[] Icd10Codes =
    [
        new("A09", "Infectious gastroenteritis and colitis, unspecified", "Certain infectious and parasitic diseases"),
        new("B34.9", "Viral infection, unspecified", "Certain infectious and parasitic diseases"),
        new("E11.9", "Type 2 diabetes mellitus without complications", "Endocrine, nutritional and metabolic diseases"),
        new("E78.5", "Hyperlipidaemia, unspecified", "Endocrine, nutritional and metabolic diseases"),
        new("F41.9", "Anxiety disorder, unspecified", "Mental and behavioural disorders"),
        new("G43.9", "Migraine, unspecified", "Diseases of the nervous system"),
        new("H66.9", "Otitis media, unspecified", "Diseases of the ear and mastoid process"),
        new("I10", "Essential primary hypertension", "Diseases of the circulatory system"),
        new("I20.9", "Angina pectoris, unspecified", "Diseases of the circulatory system"),
        new("J00", "Acute nasopharyngitis", "Diseases of the respiratory system"),
        new("J02.9", "Acute pharyngitis, unspecified", "Diseases of the respiratory system"),
        new("J06.9", "Acute upper respiratory infection, unspecified", "Diseases of the respiratory system"),
        new("J18.9", "Pneumonia, unspecified", "Diseases of the respiratory system"),
        new("J45.9", "Asthma, unspecified", "Diseases of the respiratory system"),
        new("K21.9", "Gastro-oesophageal reflux disease without oesophagitis", "Diseases of the digestive system"),
        new("M54.5", "Low back pain", "Diseases of the musculoskeletal system and connective tissue"),
        new("N39.0", "Urinary tract infection, site not specified", "Diseases of the genitourinary system"),
        new("R05", "Cough", "Symptoms, signs and abnormal clinical findings"),
        new("R50.9", "Fever, unspecified", "Symptoms, signs and abnormal clinical findings"),
        new("Z00.0", "General medical examination", "Factors influencing health status")
    ];

    public async Task<Result<CompleteConsultationResponse>> CompleteAsync(
        Guid appointmentId,
        CompleteConsultationRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCompletion(request);
        if (validationErrors.Count > 0)
        {
            return Result<CompleteConsultationResponse>.Failure(Error.Validation(validationErrors));
        }

        var appointment = await dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<CompleteConsultationResponse>.Failure(new Error("CONSULTATION_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
        {
            return Result<CompleteConsultationResponse>.Failure(new Error("CONSULTATION_STATUS_INVALID", "Cancelled or no-show appointments cannot be completed."));
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return Result<CompleteConsultationResponse>.Failure(new Error("CONSULTATION_ALREADY_COMPLETED", "Consultation is already completed."));
        }

        var existingPrescription = await dbContext.Prescriptions
            .AnyAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        if (existingPrescription)
        {
            return Result<CompleteConsultationResponse>.Failure(new Error("PRESCRIPTION_EXISTS", "Prescription already exists for this appointment."));
        }

        var icd10 = ResolveIcd10(request.Icd10Code);
        if (!string.IsNullOrWhiteSpace(request.Icd10Code) && icd10 is null)
        {
            return Result<CompleteConsultationResponse>.Failure(new Error("ICD10_NOT_FOUND", "ICD-10 code was not found."));
        }

        appointment.Status = AppointmentStatus.Completed;
        appointment.Diagnosis = request.Diagnosis.Trim();
        appointment.Icd10Code = icd10?.Code;
        appointment.Icd10Title = icd10?.Title;
        appointment.ClinicalNotes = Normalize(request.ClinicalNotes);
        appointment.FollowUpDate = request.FollowUpDate;

        var prescription = new Prescription
        {
            PrescriptionNumber = await GeneratePrescriptionNumberAsync(DateTimeOffset.UtcNow, cancellationToken),
            AppointmentId = appointment.Id,
            Appointment = appointment,
            PatientId = appointment.PatientId,
            Patient = appointment.Patient,
            DoctorId = appointment.DoctorId,
            Doctor = appointment.Doctor,
            Diagnosis = appointment.Diagnosis,
            Icd10Code = appointment.Icd10Code,
            Icd10Title = appointment.Icd10Title,
            ClinicalNotes = appointment.ClinicalNotes,
            FollowUpDate = appointment.FollowUpDate,
            IssuedAt = DateTimeOffset.UtcNow,
            ValidUntil = DateTimeOffset.UtcNow.AddDays(30),
            Status = PrescriptionStatus.Issued
        };

        var sortOrder = (short)1;
        foreach (var item in request.PrescriptionItems)
        {
            prescription.Items.Add(new PrescriptionItem
            {
                SortOrder = sortOrder++,
                MedicineName = item.MedicineName.Trim(),
                GenericName = Normalize(item.GenericName),
                Strength = Normalize(item.Strength),
                Route = Normalize(item.Route),
                Dosage = item.Dosage.Trim(),
                Frequency = item.Frequency.Trim(),
                DurationDays = item.DurationDays,
                Quantity = item.Quantity,
                Instructions = Normalize(item.Instructions),
                IsSubstitutionAllowed = item.IsSubstitutionAllowed
            });
        }

        dbContext.Prescriptions.Add(prescription);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CompleteConsultationResponse>.Success(MapCompletion(appointment, prescription));
    }

    public Task<IReadOnlyList<Icd10CodeResponse>> SearchIcd10Async(string? search, CancellationToken cancellationToken)
    {
        var query = Normalize(search);
        var results = string.IsNullOrWhiteSpace(query)
            ? Icd10Codes
            : Icd10Codes
                .Where(x =>
                    x.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || x.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || x.Chapter.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        return Task.FromResult<IReadOnlyList<Icd10CodeResponse>>(results.Take(20).ToList());
    }

    public async Task<Result<PrescriptionResponse>> GetPrescriptionByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var prescription = await PrescriptionQuery()
            .SingleOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);

        return prescription is null
            ? Result<PrescriptionResponse>.Failure(new Error("PRESCRIPTION_NOT_FOUND", "Prescription was not found."))
            : Result<PrescriptionResponse>.Success(MapPrescription(prescription));
    }

    private IQueryable<Prescription> PrescriptionQuery()
    {
        return dbContext.Prescriptions
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User)
            .Include(x => x.Items);
    }

    private async Task<string> GeneratePrescriptionNumberAsync(DateTimeOffset issuedAt, CancellationToken cancellationToken)
    {
        var datePart = issuedAt.UtcDateTime.ToString("yyyyMMdd");
        var prefix = $"RX-{datePart}-";
        var count = await dbContext.Prescriptions.CountAsync(x => x.PrescriptionNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private static Icd10CodeResponse? ResolveIcd10(string? code)
    {
        var normalizedCode = Normalize(code);
        return string.IsNullOrWhiteSpace(normalizedCode)
            ? null
            : Icd10Codes.SingleOrDefault(x => string.Equals(x.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));
    }

    private static List<ValidationError> ValidateCompletion(CompleteConsultationRequest request)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(request.Diagnosis))
        {
            errors.Add(new ValidationError(nameof(request.Diagnosis), "Diagnosis is required."));
        }

        if (request.PrescriptionItems is null || request.PrescriptionItems.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.PrescriptionItems), "At least one prescription item is required."));
            return errors;
        }

        for (var i = 0; i < request.PrescriptionItems.Count; i++)
        {
            var item = request.PrescriptionItems[i];
            var prefix = $"{nameof(request.PrescriptionItems)}[{i}]";
            if (string.IsNullOrWhiteSpace(item.MedicineName))
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.MedicineName)}", "MedicineName is required."));
            }

            if (string.IsNullOrWhiteSpace(item.Dosage))
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.Dosage)}", "Dosage is required."));
            }

            if (string.IsNullOrWhiteSpace(item.Frequency))
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.Frequency)}", "Frequency is required."));
            }

            if (item.DurationDays <= 0)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.DurationDays)}", "DurationDays must be positive."));
            }

            if (item.Quantity <= 0)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.Quantity)}", "Quantity must be positive."));
            }
        }

        return errors;
    }

    private static CompleteConsultationResponse MapCompletion(Appointment appointment, Prescription prescription)
    {
        return new CompleteConsultationResponse(
            appointment.Id,
            appointment.AppointmentNumber,
            appointment.Status.ToString(),
            appointment.Diagnosis ?? string.Empty,
            appointment.Icd10Code,
            appointment.Icd10Title,
            appointment.ClinicalNotes,
            appointment.FollowUpDate,
            MapPrescription(prescription));
    }

    private static PrescriptionResponse MapPrescription(Prescription prescription)
    {
        return new PrescriptionResponse(
            prescription.Id,
            prescription.PrescriptionNumber,
            prescription.AppointmentId,
            prescription.PatientId,
            $"{prescription.Patient.FirstName} {prescription.Patient.LastName}".Trim(),
            prescription.DoctorId,
            prescription.Doctor.User.FullName,
            prescription.IssuedAt,
            prescription.ValidUntil,
            prescription.Status.ToString(),
            prescription.Items
                .OrderBy(x => x.SortOrder)
                .Select(MapItem)
                .ToList());
    }

    private static PrescriptionItemResponse MapItem(PrescriptionItem item)
    {
        return new PrescriptionItemResponse(
            item.Id,
            item.SortOrder,
            item.MedicineName,
            item.GenericName,
            item.Strength,
            item.Route,
            item.Dosage,
            item.Frequency,
            item.DurationDays,
            item.Quantity,
            item.Instructions,
            item.IsSubstitutionAllowed);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
