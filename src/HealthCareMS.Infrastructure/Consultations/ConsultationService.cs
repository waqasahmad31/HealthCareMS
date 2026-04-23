using System.Security.Cryptography;
using System.Text;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Application.Labs;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Labs;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Infrastructure.Consultations;

public sealed class ConsultationService(
    HealthCareDbContext dbContext,
    IPrescriptionDocumentService? prescriptionDocumentService = null,
    IOptions<PrescriptionDocumentOptions>? prescriptionDocumentOptions = null,
    IConsultationSummaryDocumentService? consultationSummaryDocumentService = null) : IConsultationService
{
    private readonly PrescriptionDocumentOptions documentOptions = prescriptionDocumentOptions?.Value ?? new PrescriptionDocumentOptions();

    private static readonly DrapMedicine[] DefaultDrapMedicines =
    [
        Medicine("DRAP-ACM-500", "Panadol", "Paracetamol", "500mg", "Tablet", "GSK Pakistan", "Paracetamol,Acetaminophen"),
        Medicine("DRAP-AMX-500", "Amoxil", "Amoxicillin", "500mg", "Capsule", "GSK Pakistan", "Penicillin,Amoxicillin,Beta-lactam"),
        Medicine("DRAP-IBU-400", "Brufen", "Ibuprofen", "400mg", "Tablet", "Abbott", "Ibuprofen,NSAID,Aspirin"),
        Medicine("DRAP-CET-10", "Zyrtec", "Cetirizine", "10mg", "Tablet", "Martin Dow", "Cetirizine"),
        Medicine("DRAP-OME-20", "Losec", "Omeprazole", "20mg", "Capsule", "AstraZeneca", "Omeprazole"),
        Medicine("DRAP-MET-500", "Glucophage", "Metformin", "500mg", "Tablet", "Merck", "Metformin"),
        Medicine("DRAP-LOS-50", "Cozaar", "Losartan", "50mg", "Tablet", "MSD", "Losartan"),
        Medicine("DRAP-AML-5", "Norvasc", "Amlodipine", "5mg", "Tablet", "Pfizer", "Amlodipine"),
        Medicine("DRAP-CIP-500", "Ciproxin", "Ciprofloxacin", "500mg", "Tablet", "Bayer", "Ciprofloxacin,Quinolone"),
        Medicine("DRAP-RAN-150", "Ranitidine", "Ranitidine", "150mg", "Tablet", "Legacy", "Ranitidine", isBanned: true)
    ];

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

    public async Task<IReadOnlyList<DrapMedicineResponse>> SearchDrapMedicinesAsync(string? search, CancellationToken cancellationToken)
    {
        await EnsureDefaultDrapMedicinesAsync(cancellationToken);

        var queryText = Normalize(search);
        var query = dbContext.DrapMedicines.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(queryText))
        {
            var value = queryText.ToLowerInvariant();
            query = query.Where(x =>
                x.BrandName.ToLower().Contains(value)
                || x.GenericName.ToLower().Contains(value)
                || x.DrapRegistrationNumber.ToLower().Contains(value));
        }

        var medicines = await query
            .OrderBy(x => x.IsBanned)
            .ThenBy(x => x.BrandName)
            .Take(25)
            .ToListAsync(cancellationToken);

        return medicines.Select(MapMedicine).ToList();
    }

    public async Task<Result<IReadOnlyList<DrugAllergyWarningResponse>>> CheckDrugAllergiesAsync(
        Guid patientId,
        DrugAllergyCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PrescriptionItems is null || request.PrescriptionItems.Count == 0)
        {
            return Result<IReadOnlyList<DrugAllergyWarningResponse>>.Failure(Error.Validation([
                new ValidationError(nameof(request.PrescriptionItems), "At least one prescription item is required.")
            ]));
        }

        var patient = await dbContext.Patients
            .Include(x => x.MedicalHistory)
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        if (patient is null)
        {
            return Result<IReadOnlyList<DrugAllergyWarningResponse>>.Failure(new Error("PATIENT_NOT_FOUND", "Patient was not found."));
        }

        await EnsureDefaultDrapMedicinesAsync(cancellationToken);
        var medicines = await dbContext.DrapMedicines.AsNoTracking().ToListAsync(cancellationToken);
        var allergies = ParseAllergyTokens(patient.MedicalHistory?.Allergies);
        var warnings = BuildAllergyWarnings(request.PrescriptionItems, medicines, allergies);
        return Result<IReadOnlyList<DrugAllergyWarningResponse>>.Success(warnings);
    }

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

        var bannedMedicine = await FindBannedMedicineAsync(request.PrescriptionItems, cancellationToken);
        if (bannedMedicine is not null)
        {
            return Result<CompleteConsultationResponse>.Failure(new Error("DRAP_MEDICINE_BANNED", $"{bannedMedicine.BrandName} is marked as DRAP-banned and cannot be prescribed."));
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
        prescription.VerificationCode = GenerateVerificationCode(prescription.Id, prescription.PrescriptionNumber);
        prescription.DigitalSignature = GenerateDigitalSignature(prescription);

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

    public async Task<Result<PrescriptionVerificationResponse>> VerifyPrescriptionAsync(
        Guid prescriptionId,
        string verificationCode,
        CancellationToken cancellationToken)
    {
        var prescription = await PrescriptionQuery()
            .SingleOrDefaultAsync(x => x.Id == prescriptionId, cancellationToken);

        if (prescription is null)
        {
            return Result<PrescriptionVerificationResponse>.Failure(new Error("PRESCRIPTION_NOT_FOUND", "Prescription was not found."));
        }

        var isValid = string.Equals(prescription.VerificationCode, verificationCode, StringComparison.Ordinal);
        return Result<PrescriptionVerificationResponse>.Success(new PrescriptionVerificationResponse(
            prescription.Id,
            prescription.PrescriptionNumber,
            prescription.Status.ToString(),
            prescription.IssuedAt,
            prescription.ValidUntil,
            $"{prescription.Patient.FirstName} {prescription.Patient.LastName}".Trim(),
            prescription.Doctor.User.FullName,
            prescription.Doctor.PmdcRegistrationNumber,
            prescription.DigitalSignature,
            isValid));
    }

    public async Task<Result<PrescriptionPdfResponse>> GeneratePrescriptionPdfAsync(
        Guid prescriptionId,
        CancellationToken cancellationToken)
    {
        var prescription = await PrescriptionQuery()
            .SingleOrDefaultAsync(x => x.Id == prescriptionId, cancellationToken);

        if (prescription is null)
        {
            return Result<PrescriptionPdfResponse>.Failure(new Error("PRESCRIPTION_NOT_FOUND", "Prescription was not found."));
        }

        if (prescriptionDocumentService is null)
        {
            return Result<PrescriptionPdfResponse>.Failure(new Error("PRESCRIPTION_PDF_UNAVAILABLE", "Prescription PDF service is unavailable."));
        }

        var verificationUrl = BuildVerificationUrl(prescription);
        var pdf = prescriptionDocumentService.GeneratePrescriptionPdf(prescription, verificationUrl);
        return Result<PrescriptionPdfResponse>.Success(new PrescriptionPdfResponse(
            pdf,
            $"{prescription.PrescriptionNumber}.pdf",
            "application/pdf"));
    }

    public async Task<Result<ConsultationSummaryResponse>> GetSummaryAsync(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<ConsultationSummaryResponse>.Failure(new Error("CONSULTATION_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        var prescription = await PrescriptionQuery()
            .SingleOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        var labOrders = await LabBookingQuery()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<ConsultationSummaryResponse>.Success(MapSummary(appointment, prescription, labOrders));
    }

    public async Task<Result<ConsultationSummaryPdfResponse>> GenerateSummaryPdfAsync(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        if (consultationSummaryDocumentService is null)
        {
            return Result<ConsultationSummaryPdfResponse>.Failure(new Error("CONSULTATION_SUMMARY_PDF_UNAVAILABLE", "Consultation summary PDF service is unavailable."));
        }

        var summary = await GetSummaryAsync(appointmentId, cancellationToken);
        if (summary.IsFailure)
        {
            return Result<ConsultationSummaryPdfResponse>.Failure(summary.Error);
        }

        var pdf = consultationSummaryDocumentService.GenerateSummaryPdf(summary.Value);
        return Result<ConsultationSummaryPdfResponse>.Success(new ConsultationSummaryPdfResponse(
            pdf,
            $"{summary.Value.AppointmentNumber}-Summary.pdf",
            "application/pdf"));
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

    private IQueryable<LabSampleBooking> LabBookingQuery()
    {
        return dbContext.LabSampleBookings
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(x => x.LabTest);
    }

    private async Task EnsureDefaultDrapMedicinesAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.DrapMedicines.AnyAsync(cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.DrapMedicines.AddRange(DefaultDrapMedicines.Select(CloneMedicine));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<DrapMedicine?> FindBannedMedicineAsync(
        IReadOnlyList<PrescriptionItemRequest> items,
        CancellationToken cancellationToken)
    {
        await EnsureDefaultDrapMedicinesAsync(cancellationToken);
        var medicines = await dbContext.DrapMedicines
            .AsNoTracking()
            .Where(x => x.IsBanned)
            .ToListAsync(cancellationToken);

        return medicines.FirstOrDefault(medicine =>
            items.Any(item =>
                string.Equals(item.MedicineName, medicine.BrandName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.MedicineName, medicine.GenericName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.GenericName, medicine.GenericName, StringComparison.OrdinalIgnoreCase)));
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
            prescription.VerificationCode,
            prescription.DigitalSignature,
            prescription.Items
                .OrderBy(x => x.SortOrder)
                .Select(MapItem)
                .ToList());
    }

    private static ConsultationSummaryResponse MapSummary(
        Appointment appointment,
        Prescription? prescription,
        IReadOnlyList<LabSampleBooking> labOrders)
    {
        return new ConsultationSummaryResponse(
            appointment.Id,
            appointment.AppointmentNumber,
            appointment.PatientId,
            $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim(),
            appointment.DoctorId,
            appointment.Doctor.User.FullName,
            appointment.Status.ToString(),
            appointment.Diagnosis,
            appointment.Icd10Code,
            appointment.Icd10Title,
            appointment.ClinicalNotes,
            appointment.FollowUpDate,
            prescription is null ? null : MapPrescription(prescription),
            labOrders.Select(MapLabBooking).ToList());
    }

    private static LabBookingResponse MapLabBooking(LabSampleBooking booking)
    {
        return new LabBookingResponse(
            booking.Id,
            booking.BookingNumber,
            booking.TenantId,
            booking.PatientId,
            $"{booking.Patient.FirstName} {booking.Patient.LastName}".Trim(),
            booking.AppointmentId,
            booking.PrescriptionId,
            booking.CollectionType.ToString(),
            booking.Status.ToString(),
            booking.CollectionScheduledAt,
            booking.CollectionAddress,
            booking.SampleBarcode,
            booking.TokenNumber,
            booking.FastingVerified,
            booking.CheckedInAt,
            booking.BarcodeLabelGeneratedAt,
            booking.Notes,
            booking.SubTotal,
            booking.HomeCollectionFee,
            booking.TotalAmount,
            booking.CreatedAt,
            booking.Items
                .OrderBy(x => x.LabTest.TestName)
                .Select(MapLabBookingItem)
                .ToList());
    }

    private static LabBookingItemResponse MapLabBookingItem(LabBookingItem item)
    {
        return new LabBookingItemResponse(
            item.Id,
            item.LabTestId,
            item.LabTest.TestCode,
            item.LabTest.TestName,
            item.LabTest.Category,
            item.Price);
    }

    private static DrapMedicineResponse MapMedicine(DrapMedicine medicine)
    {
        return new DrapMedicineResponse(
            medicine.Id,
            medicine.DrapRegistrationNumber,
            medicine.BrandName,
            medicine.GenericName,
            medicine.Strength,
            medicine.DosageForm,
            medicine.Manufacturer,
            medicine.AllergenKeywords,
            medicine.IsBanned);
    }

    private static IReadOnlyList<DrugAllergyWarningResponse> BuildAllergyWarnings(
        IReadOnlyList<PrescriptionItemRequest> items,
        IReadOnlyList<DrapMedicine> medicines,
        IReadOnlyList<string> allergies)
    {
        var warnings = new List<DrugAllergyWarningResponse>();
        if (allergies.Count == 0)
        {
            return warnings;
        }

        foreach (var item in items)
        {
            var medicine = medicines.FirstOrDefault(x =>
                string.Equals(item.MedicineName, x.BrandName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.MedicineName, x.GenericName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.GenericName, x.GenericName, StringComparison.OrdinalIgnoreCase));
            var keywords = new[]
                {
                    item.MedicineName,
                    item.GenericName,
                    medicine?.GenericName,
                    medicine?.BrandName,
                    medicine?.AllergenKeywords
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(x => x!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToArray();

            foreach (var allergy in allergies)
            {
                if (keywords.Any(keyword => keyword.Contains(allergy, StringComparison.OrdinalIgnoreCase)
                    || allergy.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    warnings.Add(new DrugAllergyWarningResponse(
                        item.MedicineName,
                        item.GenericName,
                        allergy,
                        "High",
                        $"{item.MedicineName} may conflict with patient allergy '{allergy}'."));
                }
            }
        }

        return warnings;
    }

    private string BuildVerificationUrl(Prescription prescription)
    {
        var baseUrl = documentOptions.VerificationBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{prescription.Id}/verify?code={Uri.EscapeDataString(prescription.VerificationCode)}";
    }

    private static string GenerateVerificationCode(Guid prescriptionId, string prescriptionNumber)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"{prescriptionId:N}|{prescriptionNumber}"));
        return Convert.ToHexString(hash)[..24];
    }

    private static string GenerateDigitalSignature(Prescription prescription)
    {
        using var sha = SHA256.Create();
        var raw = $"{prescription.PrescriptionNumber}|{prescription.DoctorId:N}|{prescription.PatientId:N}|{prescription.IssuedAt:O}";
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }

    private static IReadOnlyList<string> ParseAllergyTokens(string? allergies)
    {
        if (string.IsNullOrWhiteSpace(allergies) || allergies == "[]")
        {
            return [];
        }

        return allergies
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal)
            .Replace("\"", string.Empty, StringComparison.Ordinal)
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DrapMedicine Medicine(
        string registrationNumber,
        string brandName,
        string genericName,
        string? strength,
        string dosageForm,
        string? manufacturer,
        string allergenKeywords,
        bool isBanned = false)
    {
        return new DrapMedicine
        {
            DrapRegistrationNumber = registrationNumber,
            BrandName = brandName,
            GenericName = genericName,
            Strength = strength,
            DosageForm = dosageForm,
            Manufacturer = manufacturer,
            AllergenKeywords = allergenKeywords,
            IsBanned = isBanned
        };
    }

    private static DrapMedicine CloneMedicine(DrapMedicine source)
    {
        return Medicine(
            source.DrapRegistrationNumber,
            source.BrandName,
            source.GenericName,
            source.Strength,
            source.DosageForm,
            source.Manufacturer,
            source.AllergenKeywords,
            source.IsBanned);
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
