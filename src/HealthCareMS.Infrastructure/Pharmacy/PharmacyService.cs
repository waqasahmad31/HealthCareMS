using System.Globalization;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Pharmacy;
using HealthCareMS.Infrastructure.Caching;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HealthCareMS.Infrastructure.Pharmacy;

public sealed partial class PharmacyService(
    HealthCareDbContext dbContext,
    IDistributedQueryCache queryCache) : IPharmacyService
{
    public async Task<IReadOnlyList<MedicineResponse>> SearchMedicinesAsync(string? search, CancellationToken cancellationToken)
    {
        var term = string.IsNullOrWhiteSpace(search) ? "any" : search.Trim().ToLowerInvariant();
        return await queryCache.GetOrCreateAsync(
            "pharmacy-medicines",
            term,
            TimeSpan.FromMinutes(5),
            async token =>
            {
                var query = MedicineQuery();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var localTerm = search.Trim().ToLowerInvariant();
                    query = query.Where(x =>
                        x.BrandName.ToLower().Contains(localTerm)
                        || x.GenericName.ToLower().Contains(localTerm)
                        || x.DrapRegistrationNumber.ToLower().Contains(localTerm)
                        || x.Barcode.ToLower().Contains(localTerm));
                }

                var medicines = await query
                    .OrderBy(x => x.BrandName)
                    .Take(100)
                    .ToListAsync(token);

                return (IReadOnlyList<MedicineResponse>)medicines.Select(Map).ToList();
            },
            cancellationToken);
    }

    public async Task<Result<MedicineResponse>> GetMedicineAsync(Guid medicineId, CancellationToken cancellationToken)
    {
        var medicine = await MedicineQuery().SingleOrDefaultAsync(x => x.Id == medicineId, cancellationToken);
        return medicine is null
            ? Result<MedicineResponse>.Failure(new Error("MEDICINE_NOT_FOUND", "Medicine was not found."))
            : Result<MedicineResponse>.Success(Map(medicine));
    }

    public async Task<Result<MedicineResponse>> CreateMedicineAsync(
        CreateMedicineRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateMedicine(request);
        if (validationErrors.Count > 0)
        {
            return Result<MedicineResponse>.Failure(Error.Validation(validationErrors));
        }

        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? GenerateBarcode(request.BrandName, request.DrapRegistrationNumber)
            : request.Barcode.Trim();
        if (await dbContext.Medicines.AnyAsync(x => x.Barcode == barcode, cancellationToken))
        {
            return Result<MedicineResponse>.Failure(new Error("MEDICINE_BARCODE_EXISTS", "Medicine barcode already exists."));
        }

        var medicine = new Medicine
        {
            TenantId = request.TenantId,
            GenericName = request.GenericName.Trim(),
            BrandName = request.BrandName.Trim(),
            DosageForm = request.DosageForm.Trim(),
            Strength = Normalize(request.Strength),
            DrapRegistrationNumber = request.DrapRegistrationNumber.Trim(),
            Manufacturer = Normalize(request.Manufacturer),
            UnitPrice = request.UnitPrice,
            UnitCostPrice = request.UnitCostPrice,
            IsControlled = request.IsControlled,
            ReorderLevel = request.ReorderLevel,
            Barcode = barcode,
            IsActive = true
        };

        dbContext.Medicines.Add(medicine);
        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("pharmacy-medicines", cancellationToken);

        return Result<MedicineResponse>.Success(Map(medicine));
    }

    public async Task<Result<MedicineResponse>> UpdateMedicineAsync(
        Guid medicineId,
        UpdateMedicineRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateMedicine(request);
        if (validationErrors.Count > 0)
        {
            return Result<MedicineResponse>.Failure(Error.Validation(validationErrors));
        }

        var medicine = await MedicineQuery().SingleOrDefaultAsync(x => x.Id == medicineId, cancellationToken);
        if (medicine is null)
        {
            return Result<MedicineResponse>.Failure(new Error("MEDICINE_NOT_FOUND", "Medicine was not found."));
        }

        var barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? medicine.Barcode
            : request.Barcode.Trim();
        var barcodeExists = await dbContext.Medicines.AnyAsync(x => x.Id != medicineId && x.Barcode == barcode, cancellationToken);
        if (barcodeExists)
        {
            return Result<MedicineResponse>.Failure(new Error("MEDICINE_BARCODE_EXISTS", "Medicine barcode already exists."));
        }

        medicine.GenericName = request.GenericName.Trim();
        medicine.BrandName = request.BrandName.Trim();
        medicine.DosageForm = request.DosageForm.Trim();
        medicine.Strength = Normalize(request.Strength);
        medicine.DrapRegistrationNumber = request.DrapRegistrationNumber.Trim();
        medicine.Manufacturer = Normalize(request.Manufacturer);
        medicine.UnitPrice = request.UnitPrice;
        medicine.UnitCostPrice = request.UnitCostPrice;
        medicine.IsControlled = request.IsControlled;
        medicine.ReorderLevel = request.ReorderLevel;
        medicine.Barcode = barcode;
        medicine.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("pharmacy-medicines", cancellationToken);

        return Result<MedicineResponse>.Success(Map(medicine));
    }

    public async Task<Result<StockBatchResponse>> CreateStockBatchAsync(
        Guid medicineId,
        CreateStockBatchRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateStockBatch(request);
        if (validationErrors.Count > 0)
        {
            return Result<StockBatchResponse>.Failure(Error.Validation(validationErrors));
        }

        var medicine = await dbContext.Medicines.SingleOrDefaultAsync(x => x.Id == medicineId, cancellationToken);
        if (medicine is null)
        {
            return Result<StockBatchResponse>.Failure(new Error("MEDICINE_NOT_FOUND", "Medicine was not found."));
        }

        var batchExists = await dbContext.StockBatches
            .AnyAsync(x => x.MedicineId == medicineId && x.BatchNumber == request.BatchNumber.Trim(), cancellationToken);
        if (batchExists)
        {
            return Result<StockBatchResponse>.Failure(new Error("STOCK_BATCH_EXISTS", "Stock batch already exists for this medicine."));
        }

        var batch = new StockBatch
        {
            TenantId = request.TenantId ?? medicine.TenantId,
            MedicineId = medicine.Id,
            SupplierId = request.SupplierId,
            BatchNumber = request.BatchNumber.Trim(),
            ManufacturedDate = request.ManufacturedDate,
            ExpiryDate = request.ExpiryDate,
            QuantityOnHand = request.QuantityOnHand,
            UnitCostPrice = request.UnitCostPrice,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        dbContext.StockBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<StockBatchResponse>.Success(Map(batch));
    }

    public async Task<Result<MedicineImportResponse>> ImportMedicinesCsvAsync(
        ImportMedicineCsvRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CsvContent))
        {
            return Result<MedicineImportResponse>.Failure(Error.Validation([
                new ValidationError(nameof(request.CsvContent), "CsvContent is required.")
            ]));
        }

        var medicines = new List<Medicine>();
        var lines = request.CsvContent
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var startIndex = lines.Count > 0 && lines[0].Contains("GenericName", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        for (var index = startIndex; index < lines.Count; index++)
        {
            var columns = SplitCsvLine(lines[index]);
            if (columns.Count < 8)
            {
                return Result<MedicineImportResponse>.Failure(new Error("MEDICINE_CSV_INVALID", "CSV rows must include GenericName, BrandName, DosageForm, Strength, DrapRegistrationNumber, Manufacturer, UnitPrice, UnitCostPrice."));
            }

            if (!decimal.TryParse(columns[6], NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice)
                || !decimal.TryParse(columns[7], NumberStyles.Number, CultureInfo.InvariantCulture, out var unitCost))
            {
                return Result<MedicineImportResponse>.Failure(new Error("MEDICINE_CSV_INVALID", "CSV price columns must be valid decimals."));
            }

            var isControlled = columns.Count > 8 && bool.TryParse(columns[8], out var controlled) && controlled;
            var reorderLevel = columns.Count > 9 && int.TryParse(columns[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedReorder)
                ? parsedReorder
                : 10;
            var barcode = columns.Count > 10 && !string.IsNullOrWhiteSpace(columns[10])
                ? columns[10].Trim()
                : GenerateBarcode(columns[1], columns[4]);

            medicines.Add(new Medicine
            {
                TenantId = request.TenantId,
                GenericName = columns[0].Trim(),
                BrandName = columns[1].Trim(),
                DosageForm = columns[2].Trim(),
                Strength = Normalize(columns[3]),
                DrapRegistrationNumber = columns[4].Trim(),
                Manufacturer = Normalize(columns[5]),
                UnitPrice = unitPrice,
                UnitCostPrice = unitCost,
                IsControlled = isControlled,
                ReorderLevel = reorderLevel,
                Barcode = barcode,
                IsActive = true
            });
        }

        var validationErrors = medicines
            .SelectMany(x => ValidateMedicine(new CreateMedicineRequest(
                x.TenantId,
                x.GenericName,
                x.BrandName,
                x.DosageForm,
                x.Strength,
                x.DrapRegistrationNumber,
                x.Manufacturer,
                x.UnitPrice,
                x.UnitCostPrice,
                x.IsControlled,
                x.ReorderLevel,
                x.Barcode)))
            .ToList();
        if (validationErrors.Count > 0)
        {
            return Result<MedicineImportResponse>.Failure(Error.Validation(validationErrors));
        }

        var barcodes = medicines.Select(x => x.Barcode).ToArray();
        var duplicateExists = await dbContext.Medicines.AnyAsync(x => barcodes.Contains(x.Barcode), cancellationToken);
        if (duplicateExists || barcodes.Length != barcodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            return Result<MedicineImportResponse>.Failure(new Error("MEDICINE_BARCODE_EXISTS", "CSV contains duplicate or existing barcodes."));
        }

        dbContext.Medicines.AddRange(medicines);
        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("pharmacy-medicines", cancellationToken);

        return Result<MedicineImportResponse>.Success(new MedicineImportResponse(
            medicines.Count,
            medicines.Select(Map).ToList()));
    }

    public async Task<Result<PharmacyStockDashboardResponse>> GetStockDashboardAsync(CancellationToken cancellationToken)
    {
        var medicines = await MedicineQuery()
            .OrderBy(x => x.BrandName)
            .Take(300)
            .ToListAsync(cancellationToken);

        var lowStock = medicines
            .Where(x => x.StockBatches.Sum(batch => batch.QuantityOnHand) <= x.ReorderLevel)
            .Select(Map)
            .ToList();

        var openAlerts = await AlertQuery()
            .Where(x => x.Status == StockAlertStatus.Open)
            .OrderByDescending(x => x.DetectedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var expiryAlerts = openAlerts
            .Where(x => x.AlertType is StockAlertType.Expiry30Days or StockAlertType.Expiry60Days or StockAlertType.Expiry90Days)
            .Select(Map)
            .ToList();

        return Result<PharmacyStockDashboardResponse>.Success(new PharmacyStockDashboardResponse(
            medicines.Count,
            lowStock.Count,
            expiryAlerts.Count,
            lowStock,
            expiryAlerts,
            openAlerts.Select(Map).ToList()));
    }

    public async Task<Result<IReadOnlyList<StockBatchResponse>>> GetStockBatchesAsync(
        Guid medicineId,
        CancellationToken cancellationToken)
    {
        var medicineExists = await dbContext.Medicines.AnyAsync(x => x.Id == medicineId, cancellationToken);
        if (!medicineExists)
        {
            return Result<IReadOnlyList<StockBatchResponse>>.Failure(new Error("MEDICINE_NOT_FOUND", "Medicine was not found."));
        }

        var batches = await dbContext.StockBatches
            .Where(x => x.MedicineId == medicineId)
            .OrderBy(x => x.ReceivedAt)
            .ThenBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<StockBatchResponse>>.Success(batches.Select(Map).ToList());
    }

    public async Task<Result<StockAdjustmentResponse>> AdjustStockBatchAsync(
        Guid stockBatchId,
        AdjustStockBatchRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateAdjustment(request);
        if (validationErrors.Count > 0)
        {
            return Result<StockAdjustmentResponse>.Failure(Error.Validation(validationErrors));
        }

        if (!Enum.TryParse<StockAdjustmentType>(request.AdjustmentType, ignoreCase: true, out var adjustmentType))
        {
            return Result<StockAdjustmentResponse>.Failure(new Error("STOCK_ADJUSTMENT_TYPE_INVALID", "AdjustmentType is invalid."));
        }

        var batch = await dbContext.StockBatches
            .Include(x => x.Medicine)
            .SingleOrDefaultAsync(x => x.Id == stockBatchId, cancellationToken);

        if (batch is null)
        {
            return Result<StockAdjustmentResponse>.Failure(new Error("STOCK_BATCH_NOT_FOUND", "Stock batch was not found."));
        }

        var result = ApplyAdjustment(batch, request.QuantityDelta, adjustmentType, request.Reason);
        if (result.IsFailure)
        {
            return Result<StockAdjustmentResponse>.Failure(result.Error);
        }

        dbContext.StockAdjustments.Add(result.Value);
        await ResolveLowStockIfRecoveredAsync(batch.MedicineId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentResponse>.Success(Map(result.Value));
    }

    public async Task<Result<FifoBatchSelectionResponse>> GetFifoBatchSelectionAsync(
        Guid medicineId,
        int quantityRequired,
        CancellationToken cancellationToken)
    {
        if (quantityRequired <= 0)
        {
            return Result<FifoBatchSelectionResponse>.Failure(new Error("FIFO_QUANTITY_INVALID", "QuantityRequired must be greater than zero."));
        }

        var medicine = await MedicineQuery().SingleOrDefaultAsync(x => x.Id == medicineId, cancellationToken);
        if (medicine is null)
        {
            return Result<FifoBatchSelectionResponse>.Failure(new Error("MEDICINE_NOT_FOUND", "Medicine was not found."));
        }

        var selection = BuildFifoSelection(medicine, quantityRequired);
        return Result<FifoBatchSelectionResponse>.Success(selection);
    }

    public async Task<Result<FifoDispenseResponse>> DispenseFifoAsync(
        Guid medicineId,
        FifoDispenseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.QuantityRequired <= 0)
        {
            return Result<FifoDispenseResponse>.Failure(new Error("FIFO_QUANTITY_INVALID", "QuantityRequired must be greater than zero."));
        }

        var medicine = await MedicineQuery().SingleOrDefaultAsync(x => x.Id == medicineId, cancellationToken);
        if (medicine is null)
        {
            return Result<FifoDispenseResponse>.Failure(new Error("MEDICINE_NOT_FOUND", "Medicine was not found."));
        }

        var selection = BuildFifoSelection(medicine, request.QuantityRequired);
        if (!selection.IsFulfillable)
        {
            return Result<FifoDispenseResponse>.Failure(new Error("FIFO_STOCK_INSUFFICIENT", "Not enough stock is available for FIFO dispense."));
        }

        var adjustments = new List<StockAdjustment>();
        foreach (var item in selection.Batches)
        {
            var batch = medicine.StockBatches.Single(x => x.Id == item.StockBatchId);
            var adjustment = ApplyAdjustment(
                batch,
                -item.QuantitySelected,
                StockAdjustmentType.Dispense,
                Normalize(request.Reason) ?? "FIFO dispense");
            if (adjustment.IsFailure)
            {
                return Result<FifoDispenseResponse>.Failure(adjustment.Error);
            }

            adjustments.Add(adjustment.Value);
        }

        dbContext.StockAdjustments.AddRange(adjustments);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<FifoDispenseResponse>.Success(new FifoDispenseResponse(
            medicine.Id,
            medicine.BrandName,
            request.QuantityRequired,
            selection.QuantitySelected,
            selection.Batches,
            adjustments.Select(Map).ToList()));
    }

    public async Task<Result<IReadOnlyList<StockAlertResponse>>> RunStockAlertScanAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime.Date);
        var medicines = await MedicineQuery().ToListAsync(cancellationToken);
        var created = new List<StockAlert>();

        foreach (var medicine in medicines)
        {
            var stockOnHand = medicine.StockBatches.Sum(x => x.QuantityOnHand);
            if (stockOnHand <= medicine.ReorderLevel)
            {
                var alert = await EnsureAlertAsync(
                    medicine,
                    stockBatch: null,
                    StockAlertType.LowStock,
                    stockOnHand <= 0 ? "Critical" : "Warning",
                    $"{medicine.BrandName} stock is {stockOnHand}, below reorder level {medicine.ReorderLevel}.",
                    stockOnHand,
                    medicine.ReorderLevel,
                    expiryDate: null,
                    now,
                    cancellationToken);
                if (alert is not null)
                {
                    created.Add(alert);
                }
            }

            foreach (var batch in medicine.StockBatches.Where(x => x.QuantityOnHand > 0))
            {
                var alertType = ResolveExpiryAlertType(today, batch.ExpiryDate);
                if (alertType is null)
                {
                    continue;
                }

                var days = (batch.ExpiryDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;
                var alert = await EnsureAlertAsync(
                    medicine,
                    batch,
                    alertType.Value,
                    days <= 30 ? "Critical" : days <= 60 ? "Warning" : "Info",
                    $"{medicine.BrandName} batch {batch.BatchNumber} expires in {days} days.",
                    batch.QuantityOnHand,
                    thresholdQuantity: null,
                    batch.ExpiryDate,
                    now,
                    cancellationToken);
                if (alert is not null)
                {
                    created.Add(alert);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<StockAlertResponse>>.Success(created.Select(Map).ToList());
    }

    public async Task<Result<DispensePrescriptionLookupResponse>> GetPrescriptionForDispensingAsync(
        Guid prescriptionId,
        string verificationCode,
        CancellationToken cancellationToken)
    {
        var prescription = await PrescriptionQuery()
            .SingleOrDefaultAsync(x => x.Id == prescriptionId, cancellationToken);
        if (prescription is null)
        {
            return Result<DispensePrescriptionLookupResponse>.Failure(new Error("PRESCRIPTION_NOT_FOUND", "Prescription was not found."));
        }

        var alreadyDispensed = await dbContext.PrescriptionDispenses
            .AnyAsync(x => x.PrescriptionId == prescriptionId && x.Status == PrescriptionDispenseStatus.Completed, cancellationToken);
        var verificationMatched = string.Equals(prescription.VerificationCode, verificationCode, StringComparison.Ordinal);
        var validationMessages = ValidatePrescriptionForDispense(prescription, verificationMatched, alreadyDispensed).ToList();
        var items = new List<DispensePrescriptionLookupItemResponse>();

        foreach (var item in prescription.Items.OrderBy(x => x.SortOrder))
        {
            var suggested = await FindSuggestedMedicinesAsync(item, cancellationToken);
            if (!suggested.Any(x => x.IsAvailable))
            {
                validationMessages.Add($"No available stock found for {item.MedicineName}.");
            }

            items.Add(new DispensePrescriptionLookupItemResponse(
                item.Id,
                item.MedicineName,
                item.GenericName,
                item.Strength,
                item.Dosage,
                item.Frequency,
                item.DurationDays,
                item.Quantity,
                suggested));
        }

        return Result<DispensePrescriptionLookupResponse>.Success(new DispensePrescriptionLookupResponse(
            prescription.Id,
            prescription.PrescriptionNumber,
            prescription.PatientId,
            $"{prescription.Patient.FirstName} {prescription.Patient.LastName}".Trim(),
            prescription.DoctorId,
            prescription.Doctor.User.FullName,
            prescription.Doctor.PmdcRegistrationNumber,
            IsDoctorLicenseValid(prescription),
            prescription.Status.ToString(),
            prescription.IssuedAt,
            prescription.ValidUntil,
            verificationMatched,
            validationMessages.Count == 0,
            alreadyDispensed,
            validationMessages,
            items));
    }

    public async Task<Result<PrescriptionDispenseResponse>> DispensePrescriptionAsync(
        Guid prescriptionId,
        DispensePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var requestErrors = ValidateDispenseRequest(request);
        if (requestErrors.Count > 0)
        {
            return Result<PrescriptionDispenseResponse>.Failure(Error.Validation(requestErrors));
        }

        var prescription = await PrescriptionQuery()
            .SingleOrDefaultAsync(x => x.Id == prescriptionId, cancellationToken);
        if (prescription is null)
        {
            return Result<PrescriptionDispenseResponse>.Failure(new Error("PRESCRIPTION_NOT_FOUND", "Prescription was not found."));
        }

        var alreadyDispensed = await dbContext.PrescriptionDispenses
            .AnyAsync(x => x.PrescriptionId == prescriptionId && x.Status == PrescriptionDispenseStatus.Completed, cancellationToken);
        var verificationMatched = string.Equals(prescription.VerificationCode, request.VerificationCode, StringComparison.Ordinal);
        var validationMessages = ValidatePrescriptionForDispense(prescription, verificationMatched, alreadyDispensed).ToList();
        if (validationMessages.Count > 0)
        {
            return Result<PrescriptionDispenseResponse>.Failure(new Error("PRESCRIPTION_DISPENSE_INVALID", string.Join(" ", validationMessages)));
        }

        var requestItemIds = request.Items.Select(x => x.PrescriptionItemId).ToArray();
        if (requestItemIds.Length != requestItemIds.Distinct().Count())
        {
            return Result<PrescriptionDispenseResponse>.Failure(new Error("PRESCRIPTION_DISPENSE_DUPLICATE_ITEM", "Prescription dispense request contains duplicate items."));
        }

        var prescriptionItems = prescription.Items.ToDictionary(x => x.Id);
        if (request.Items.Any(x => !prescriptionItems.ContainsKey(x.PrescriptionItemId)))
        {
            return Result<PrescriptionDispenseResponse>.Failure(new Error("PRESCRIPTION_ITEM_NOT_FOUND", "One or more prescription items were not found."));
        }

        var now = DateTimeOffset.UtcNow;
        var dispenseNumber = await GenerateDispenseNumberAsync(now, cancellationToken);
        var dispense = new PrescriptionDispense
        {
            TenantId = null,
            DispenseNumber = dispenseNumber,
            ReceiptNumber = dispenseNumber.Replace("DSP-", "RCT-", StringComparison.Ordinal),
            PrescriptionId = prescription.Id,
            Prescription = prescription,
            PatientId = prescription.PatientId,
            Patient = prescription.Patient,
            DoctorId = prescription.DoctorId,
            Doctor = prescription.Doctor,
            VerificationCode = request.VerificationCode.Trim(),
            Status = PrescriptionDispenseStatus.Completed,
            DispensedAt = now,
            Notes = Normalize(request.Notes)
        };

        var adjustments = new List<StockAdjustment>();
        foreach (var requestedItem in request.Items)
        {
            var prescriptionItem = prescriptionItems[requestedItem.PrescriptionItemId];
            var prescribedQuantity = (int)Math.Ceiling(prescriptionItem.Quantity);
            if (requestedItem.QuantityToDispense > prescribedQuantity)
            {
                return Result<PrescriptionDispenseResponse>.Failure(new Error("PRESCRIPTION_DISPENSE_QUANTITY_INVALID", $"{prescriptionItem.MedicineName} dispense quantity exceeds prescription quantity."));
            }

            var medicine = await MedicineQuery()
                .SingleOrDefaultAsync(x => x.Id == requestedItem.MedicineId && x.IsActive, cancellationToken);
            if (medicine is null)
            {
                return Result<PrescriptionDispenseResponse>.Failure(new Error("MEDICINE_NOT_FOUND", "Dispensed medicine was not found."));
            }

            var selection = BuildFifoSelection(medicine, requestedItem.QuantityToDispense);
            if (!selection.IsFulfillable)
            {
                return Result<PrescriptionDispenseResponse>.Failure(new Error("PRESCRIPTION_DISPENSE_STOCK_INSUFFICIENT", $"{medicine.BrandName} does not have enough stock for dispense."));
            }

            var lineTotal = requestedItem.QuantityToDispense * medicine.UnitPrice;
            var dispenseItem = new PrescriptionDispenseItem
            {
                PrescriptionItemId = prescriptionItem.Id,
                PrescriptionItem = prescriptionItem,
                MedicineId = medicine.Id,
                Medicine = medicine,
                PrescribedMedicineName = prescriptionItem.MedicineName,
                DispensedMedicineName = medicine.BrandName,
                QuantityPrescribed = prescriptionItem.Quantity,
                QuantityDispensed = requestedItem.QuantityToDispense,
                UnitPrice = medicine.UnitPrice,
                LineTotal = lineTotal
            };

            foreach (var selectedBatch in selection.Batches)
            {
                var batch = medicine.StockBatches.Single(x => x.Id == selectedBatch.StockBatchId);
                var adjustment = ApplyAdjustment(
                    batch,
                    -selectedBatch.QuantitySelected,
                    StockAdjustmentType.Dispense,
                    $"Prescription {prescription.PrescriptionNumber} dispense {dispenseNumber}");
                if (adjustment.IsFailure)
                {
                    return Result<PrescriptionDispenseResponse>.Failure(adjustment.Error);
                }

                adjustments.Add(adjustment.Value);
                dispenseItem.Batches.Add(new PrescriptionDispenseBatch
                {
                    StockBatchId = batch.Id,
                    StockBatch = batch,
                    BatchNumber = batch.BatchNumber,
                    QuantityDispensed = selectedBatch.QuantitySelected
                });
            }

            dispense.Items.Add(dispenseItem);
            dispense.SubTotal += lineTotal;
        }

        dispense.TotalAmount = dispense.SubTotal;
        dbContext.PrescriptionDispenses.Add(dispense);
        dbContext.StockAdjustments.AddRange(adjustments);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await DispenseQuery()
            .SingleAsync(x => x.Id == dispense.Id, cancellationToken);
        return Result<PrescriptionDispenseResponse>.Success(Map(saved));
    }

    public async Task<Result<IReadOnlyList<PrescriptionDispenseResponse>>> GetDispensingHistoryAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var query = DispenseQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.DispenseNumber.ToLower().Contains(term)
                || x.ReceiptNumber.ToLower().Contains(term)
                || x.Prescription.PrescriptionNumber.ToLower().Contains(term)
                || x.Patient.FirstName.ToLower().Contains(term)
                || x.Patient.LastName.ToLower().Contains(term)
                || x.Items.Any(item => item.DispensedMedicineName.ToLower().Contains(term)));
        }

        var dispenses = await query
            .OrderByDescending(x => x.DispensedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PrescriptionDispenseResponse>>.Success(dispenses.Select(Map).ToList());
    }

    public async Task<Result<DispenseReceiptPdfResponse>> GenerateDispenseReceiptPdfAsync(
        Guid dispenseId,
        CancellationToken cancellationToken)
    {
        var dispense = await DispenseQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == dispenseId, cancellationToken);
        if (dispense is null)
        {
            return Result<DispenseReceiptPdfResponse>.Failure(new Error("DISPENSE_NOT_FOUND", "Dispense record was not found."));
        }

        var pdf = GenerateReceiptPdf(dispense);
        return Result<DispenseReceiptPdfResponse>.Success(new DispenseReceiptPdfResponse(
            pdf,
            $"{dispense.ReceiptNumber}.pdf",
            "application/pdf"));
    }

    public async Task<Result<PharmacyOrderResponse>> CreateOrderAsync(
        CreatePharmacyOrderRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateOrder(request);
        if (validationErrors.Count > 0)
        {
            return Result<PharmacyOrderResponse>.Failure(Error.Validation(validationErrors));
        }

        var patient = await dbContext.Patients
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_PATIENT_NOT_FOUND", "Patient was not found."));
        }

        Prescription? prescription = null;
        if (request.PrescriptionId.HasValue)
        {
            prescription = await dbContext.Prescriptions
                .SingleOrDefaultAsync(x => x.Id == request.PrescriptionId.Value, cancellationToken);
            if (prescription is null || prescription.PatientId != request.PatientId)
            {
                return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_PRESCRIPTION_INVALID", "Prescription was not found for this patient."));
            }
        }

        var itemIds = request.Items.Select(x => x.MedicineId).Distinct().ToArray();
        var medicines = await dbContext.Medicines
            .Include(x => x.StockBatches)
            .Where(x => itemIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);
        if (medicines.Count != itemIds.Length)
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_MEDICINE_NOT_FOUND", "One or more medicines were not found."));
        }

        var now = DateTimeOffset.UtcNow;
        var order = new PharmacyOrder
        {
            TenantId = request.TenantId,
            OrderNumber = await GenerateOrderNumberAsync(now, cancellationToken),
            PatientId = patient.Id,
            Patient = patient,
            PrescriptionId = prescription?.Id,
            Prescription = prescription,
            Status = PharmacyOrderStatus.Placed,
            OrderedAt = now,
            DeliveryAddress = request.DeliveryAddress.Trim(),
            DeliveryWindowStart = request.DeliveryWindowStart?.ToUniversalTime(),
            DeliveryWindowEnd = request.DeliveryWindowEnd?.ToUniversalTime(),
            PrescriptionUploadFileName = Normalize(request.PrescriptionUploadFileName),
            PrescriptionUploadContentType = Normalize(request.PrescriptionUploadContentType),
            PrescriptionUploadContent = request.PrescriptionUploadContent,
            PatientNotes = Normalize(request.PatientNotes),
            DeliveryFee = 250m
        };

        foreach (var item in request.Items)
        {
            var medicine = medicines.Single(x => x.Id == item.MedicineId);
            var lineTotal = item.Quantity * medicine.UnitPrice;
            order.Items.Add(new PharmacyOrderItem
            {
                MedicineId = medicine.Id,
                Medicine = medicine,
                MedicineName = medicine.BrandName,
                Quantity = item.Quantity,
                UnitPrice = medicine.UnitPrice,
                LineTotal = lineTotal
            });
            order.SubTotal += lineTotal;
        }

        order.TotalAmount = order.SubTotal + order.DeliveryFee;
        dbContext.PharmacyOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await OrderQuery().SingleAsync(x => x.Id == order.Id, cancellationToken);
        return Result<PharmacyOrderResponse>.Success(Map(saved));
    }

    public async Task<Result<PharmacyOrderResponse>> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await OrderQuery().AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        return order is null
            ? Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_NOT_FOUND", "Pharmacy order was not found."))
            : Result<PharmacyOrderResponse>.Success(Map(order));
    }

    public async Task<Result<IReadOnlyList<PharmacyOrderResponse>>> GetOrdersAsync(
        string? status,
        Guid? patientId,
        Guid? deliveryAgentUserId,
        CancellationToken cancellationToken)
    {
        var query = OrderQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PharmacyOrderStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                return Result<IReadOnlyList<PharmacyOrderResponse>>.Failure(new Error("PHARMACY_ORDER_STATUS_INVALID", "Status is invalid."));
            }

            query = query.Where(x => x.Status == parsedStatus);
        }

        if (patientId.HasValue)
        {
            query = query.Where(x => x.PatientId == patientId.Value);
        }

        if (deliveryAgentUserId.HasValue)
        {
            query = query.Where(x => x.DeliveryAgentUserId == deliveryAgentUserId.Value);
        }

        var orders = await query
            .OrderByDescending(x => x.OrderedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PharmacyOrderResponse>>.Success(orders.Select(Map).ToList());
    }

    public async Task<Result<PharmacyOrderResponse>> ConfirmOrderAsync(
        Guid orderId,
        ConfirmPharmacyOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await OrderQuery().SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_NOT_FOUND", "Pharmacy order was not found."));
        }

        if (order.Status != PharmacyOrderStatus.Placed)
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_STATUS_INVALID", "Only placed orders can be confirmed."));
        }

        if (request.DeliveryAgentUserId.HasValue)
        {
            var agentExists = await dbContext.Users.AnyAsync(x => x.Id == request.DeliveryAgentUserId.Value && x.IsActive, cancellationToken);
            if (!agentExists)
            {
                return Result<PharmacyOrderResponse>.Failure(new Error("DELIVERY_AGENT_NOT_FOUND", "Delivery agent was not found."));
            }
        }

        var adjustments = new List<StockAdjustment>();
        foreach (var orderItem in order.Items)
        {
            var medicine = await MedicineQuery().SingleAsync(x => x.Id == orderItem.MedicineId, cancellationToken);
            var selection = BuildFifoSelection(medicine, orderItem.Quantity);
            if (!selection.IsFulfillable)
            {
                return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_STOCK_INSUFFICIENT", $"{medicine.BrandName} does not have enough stock."));
            }

            foreach (var selectedBatch in selection.Batches)
            {
                var batch = medicine.StockBatches.Single(x => x.Id == selectedBatch.StockBatchId);
                var adjustment = ApplyAdjustment(
                    batch,
                    -selectedBatch.QuantitySelected,
                    StockAdjustmentType.Dispense,
                    $"Online order {order.OrderNumber} confirmed");
                if (adjustment.IsFailure)
                {
                    return Result<PharmacyOrderResponse>.Failure(adjustment.Error);
                }

                adjustments.Add(adjustment.Value);
            }
        }

        var now = DateTimeOffset.UtcNow;
        order.ReviewedAt = now;
        order.ConfirmedAt = now;
        order.PharmacistNotes = Normalize(request.PharmacistNotes);
        order.Status = request.DeliveryAgentUserId.HasValue
            ? PharmacyOrderStatus.AssignedForDelivery
            : PharmacyOrderStatus.Confirmed;
        order.DeliveryAgentUserId = request.DeliveryAgentUserId;
        order.AssignedAt = request.DeliveryAgentUserId.HasValue ? now : null;
        dbContext.StockAdjustments.AddRange(adjustments);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await OrderQuery().SingleAsync(x => x.Id == order.Id, cancellationToken);
        return Result<PharmacyOrderResponse>.Success(Map(saved));
    }

    public async Task<Result<PharmacyOrderResponse>> AssignDeliveryAgentAsync(
        Guid orderId,
        AssignDeliveryAgentRequest request,
        CancellationToken cancellationToken)
    {
        var order = await OrderQuery().SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_NOT_FOUND", "Pharmacy order was not found."));
        }

        var agent = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == request.DeliveryAgentUserId && x.IsActive, cancellationToken);
        if (agent is null)
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("DELIVERY_AGENT_NOT_FOUND", "Delivery agent was not found."));
        }

        if (order.Status is PharmacyOrderStatus.Placed or PharmacyOrderStatus.Cancelled or PharmacyOrderStatus.Rejected or PharmacyOrderStatus.Delivered)
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_STATUS_INVALID", "Order cannot be assigned in its current status."));
        }

        order.DeliveryAgentUserId = agent.Id;
        order.DeliveryAgentUser = agent;
        order.AssignedAt = DateTimeOffset.UtcNow;
        order.Status = PharmacyOrderStatus.AssignedForDelivery;
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await OrderQuery().SingleAsync(x => x.Id == order.Id, cancellationToken);
        return Result<PharmacyOrderResponse>.Success(Map(saved));
    }

    public async Task<Result<PharmacyOrderResponse>> UpdateOrderStatusAsync(
        Guid orderId,
        UpdatePharmacyOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PharmacyOrderStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_STATUS_INVALID", "Status is invalid."));
        }

        var order = await OrderQuery().SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_NOT_FOUND", "Pharmacy order was not found."));
        }

        if (!CanMoveOrderTo(order.Status, newStatus))
        {
            return Result<PharmacyOrderResponse>.Failure(new Error("PHARMACY_ORDER_STATUS_INVALID", $"Cannot move order from {order.Status} to {newStatus}."));
        }

        var now = DateTimeOffset.UtcNow;
        order.Status = newStatus;
        order.PharmacistNotes = Normalize(request.Notes) ?? order.PharmacistNotes;
        if (newStatus == PharmacyOrderStatus.Dispatched)
        {
            order.DispatchedAt = now;
        }

        if (newStatus == PharmacyOrderStatus.Delivered)
        {
            order.DeliveredAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var saved = await OrderQuery().SingleAsync(x => x.Id == order.Id, cancellationToken);
        return Result<PharmacyOrderResponse>.Success(Map(saved));
    }

    private IQueryable<Medicine> MedicineQuery()
    {
        return dbContext.Medicines
            .Include(x => x.StockBatches);
    }

    private IQueryable<StockAlert> AlertQuery()
    {
        return dbContext.StockAlerts
            .Include(x => x.Medicine)
            .Include(x => x.StockBatch);
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

    private IQueryable<PrescriptionDispense> DispenseQuery()
    {
        return dbContext.PrescriptionDispenses
            .Include(x => x.Prescription)
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(x => x.Medicine)
            .Include(x => x.Items)
            .ThenInclude(x => x.PrescriptionItem)
            .Include(x => x.Items)
            .ThenInclude(x => x.Batches)
            .ThenInclude(x => x.StockBatch);
    }

    private IQueryable<PharmacyOrder> OrderQuery()
    {
        return dbContext.PharmacyOrders
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Prescription)
            .Include(x => x.DeliveryAgentUser)
            .Include(x => x.Items)
            .ThenInclude(x => x.Medicine);
    }

    private static List<ValidationError> ValidateMedicine(CreateMedicineRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.GenericName, nameof(request.GenericName), errors);
        Required(request.BrandName, nameof(request.BrandName), errors);
        Required(request.DosageForm, nameof(request.DosageForm), errors);
        Required(request.DrapRegistrationNumber, nameof(request.DrapRegistrationNumber), errors);
        if (request.UnitPrice < 0)
        {
            errors.Add(new ValidationError(nameof(request.UnitPrice), "UnitPrice cannot be negative."));
        }

        if (request.UnitCostPrice < 0)
        {
            errors.Add(new ValidationError(nameof(request.UnitCostPrice), "UnitCostPrice cannot be negative."));
        }

        if (request.ReorderLevel < 0)
        {
            errors.Add(new ValidationError(nameof(request.ReorderLevel), "ReorderLevel cannot be negative."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateCreateOrder(CreatePharmacyOrderRequest request)
    {
        var errors = new List<ValidationError>();
        if (request.PatientId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(request.PatientId), "PatientId is required."));
        }

        Required(request.DeliveryAddress, nameof(request.DeliveryAddress), errors);
        if (!string.IsNullOrWhiteSpace(request.DeliveryAddress) && request.DeliveryAddress.Trim().Length > 1000)
        {
            errors.Add(new ValidationError(nameof(request.DeliveryAddress), "DeliveryAddress cannot exceed 1000 characters."));
        }

        if (request.DeliveryWindowStart.HasValue && request.DeliveryWindowEnd.HasValue
            && request.DeliveryWindowEnd <= request.DeliveryWindowStart)
        {
            errors.Add(new ValidationError(nameof(request.DeliveryWindowEnd), "DeliveryWindowEnd must be after DeliveryWindowStart."));
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.Items), "At least one order item is required."));
            return errors;
        }

        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            var prefix = $"{nameof(request.Items)}[{index}]";
            if (item.MedicineId == Guid.Empty)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.MedicineId)}", "MedicineId is required."));
            }

            if (item.Quantity <= 0)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.Quantity)}", "Quantity must be greater than zero."));
            }
        }

        if (request.Items.Select(x => x.MedicineId).Distinct().Count() != request.Items.Count)
        {
            errors.Add(new ValidationError(nameof(request.Items), "Duplicate medicines are not allowed in the same order."));
        }

        if (!request.PrescriptionId.HasValue && (request.PrescriptionUploadContent is null || request.PrescriptionUploadContent.Length == 0))
        {
            errors.Add(new ValidationError(nameof(request.PrescriptionId), "PrescriptionId or prescription upload is required."));
        }

        if (request.PrescriptionUploadContent?.Length > 2 * 1024 * 1024)
        {
            errors.Add(new ValidationError(nameof(request.PrescriptionUploadContent), "Prescription upload cannot exceed 2 MB."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateDispenseRequest(DispensePrescriptionRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.VerificationCode, nameof(request.VerificationCode), errors);
        if (request.Items is null || request.Items.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.Items), "At least one prescription item is required."));
            return errors;
        }

        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            var prefix = $"{nameof(request.Items)}[{index}]";
            if (item.PrescriptionItemId == Guid.Empty)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.PrescriptionItemId)}", "PrescriptionItemId is required."));
            }

            if (item.MedicineId == Guid.Empty)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.MedicineId)}", "MedicineId is required."));
            }

            if (item.QuantityToDispense <= 0)
            {
                errors.Add(new ValidationError($"{prefix}.{nameof(item.QuantityToDispense)}", "QuantityToDispense must be greater than zero."));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > 1000)
        {
            errors.Add(new ValidationError(nameof(request.Notes), "Notes cannot exceed 1000 characters."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateMedicine(UpdateMedicineRequest request)
    {
        return ValidateMedicine(new CreateMedicineRequest(
            null,
            request.GenericName,
            request.BrandName,
            request.DosageForm,
            request.Strength,
            request.DrapRegistrationNumber,
            request.Manufacturer,
            request.UnitPrice,
            request.UnitCostPrice,
            request.IsControlled,
            request.ReorderLevel,
            request.Barcode));
    }

    private static List<ValidationError> ValidateStockBatch(CreateStockBatchRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.BatchNumber, nameof(request.BatchNumber), errors);
        if (request.ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            errors.Add(new ValidationError(nameof(request.ExpiryDate), "ExpiryDate must be in the future."));
        }

        if (request.QuantityOnHand < 0)
        {
            errors.Add(new ValidationError(nameof(request.QuantityOnHand), "QuantityOnHand cannot be negative."));
        }

        if (request.UnitCostPrice < 0)
        {
            errors.Add(new ValidationError(nameof(request.UnitCostPrice), "UnitCostPrice cannot be negative."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateAdjustment(AdjustStockBatchRequest request)
    {
        var errors = new List<ValidationError>();
        if (request.QuantityDelta == 0)
        {
            errors.Add(new ValidationError(nameof(request.QuantityDelta), "QuantityDelta cannot be zero."));
        }

        Required(request.AdjustmentType, nameof(request.AdjustmentType), errors);
        Required(request.Reason, nameof(request.Reason), errors);
        if (!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Trim().Length > 1000)
        {
            errors.Add(new ValidationError(nameof(request.Reason), "Reason cannot exceed 1000 characters."));
        }

        return errors;
    }

    private async Task<IReadOnlyList<MedicineAvailabilityResponse>> FindSuggestedMedicinesAsync(
        PrescriptionItem item,
        CancellationToken cancellationToken)
    {
        var medicines = await MedicineQuery()
            .Where(x => x.IsActive)
            .OrderBy(x => x.BrandName)
            .Take(300)
            .ToListAsync(cancellationToken);

        return medicines
            .Where(x => IsSuggestedMedicine(x, item))
            .Select(x => new MedicineAvailabilityResponse(
                x.Id,
                x.BrandName,
                x.Strength,
                x.UnitPrice,
                CalculateUsableStock(x),
                CalculateUsableStock(x) >= Math.Ceiling(item.Quantity)))
            .OrderByDescending(x => x.IsAvailable)
            .ThenBy(x => x.MedicineName)
            .Take(8)
            .ToList();
    }

    private static IReadOnlyList<string> ValidatePrescriptionForDispense(
        Prescription prescription,
        bool verificationMatched,
        bool alreadyDispensed)
    {
        var messages = new List<string>();
        if (!verificationMatched)
        {
            messages.Add("Prescription verification code is invalid.");
        }

        if (alreadyDispensed)
        {
            messages.Add("Prescription has already been dispensed.");
        }

        if (prescription.Status != PrescriptionStatus.Issued)
        {
            messages.Add($"Prescription status is {prescription.Status}.");
        }

        if (prescription.ValidUntil < DateTimeOffset.UtcNow)
        {
            messages.Add("Prescription is expired.");
        }

        if (!IsDoctorLicenseValid(prescription))
        {
            messages.Add("Doctor license is not valid for dispensing.");
        }

        return messages;
    }

    private static bool IsDoctorLicenseValid(Prescription prescription)
    {
        return prescription.Doctor.IsActive
            && prescription.Doctor.IsVerified
            && !string.IsNullOrWhiteSpace(prescription.Doctor.PmdcRegistrationNumber);
    }

    private static bool IsSuggestedMedicine(Medicine medicine, PrescriptionItem item)
    {
        var terms = new[]
            {
                item.MedicineName,
                item.GenericName,
                item.Strength
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();

        if (terms.Length == 0)
        {
            return false;
        }

        return terms.Any(term =>
            medicine.BrandName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || medicine.GenericName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || term.Contains(medicine.BrandName, StringComparison.OrdinalIgnoreCase)
            || term.Contains(medicine.GenericName, StringComparison.OrdinalIgnoreCase));
    }

    private static int CalculateUsableStock(Medicine medicine)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return medicine.StockBatches
            .Where(x => x.QuantityOnHand > 0 && x.ExpiryDate > today)
            .Sum(x => x.QuantityOnHand);
    }

    private static bool CanMoveOrderTo(PharmacyOrderStatus currentStatus, PharmacyOrderStatus newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (PharmacyOrderStatus.Confirmed, PharmacyOrderStatus.Prepared) => true,
            (PharmacyOrderStatus.AssignedForDelivery, PharmacyOrderStatus.Prepared) => true,
            (PharmacyOrderStatus.Prepared, PharmacyOrderStatus.Dispatched) => true,
            (PharmacyOrderStatus.AssignedForDelivery, PharmacyOrderStatus.Dispatched) => true,
            (PharmacyOrderStatus.Dispatched, PharmacyOrderStatus.Delivered) => true,
            (_, PharmacyOrderStatus.Cancelled) when currentStatus is not PharmacyOrderStatus.Delivered => true,
            (_, PharmacyOrderStatus.Rejected) when currentStatus == PharmacyOrderStatus.Placed => true,
            _ => currentStatus == newStatus
        };
    }

    private static FifoBatchSelectionResponse BuildFifoSelection(Medicine medicine, int quantityRequired)
    {
        var remaining = quantityRequired;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var selected = new List<FifoBatchSelectionItemResponse>();

        foreach (var batch in medicine.StockBatches
            .Where(x => x.QuantityOnHand > 0 && x.ExpiryDate > today)
            .OrderBy(x => x.ReceivedAt)
            .ThenBy(x => x.ExpiryDate)
            .ThenBy(x => x.BatchNumber))
        {
            if (remaining <= 0)
            {
                break;
            }

            var quantity = Math.Min(batch.QuantityOnHand, remaining);
            selected.Add(new FifoBatchSelectionItemResponse(
                batch.Id,
                batch.BatchNumber,
                batch.ReceivedAt,
                batch.ExpiryDate,
                batch.QuantityOnHand,
                quantity));
            remaining -= quantity;
        }

        var selectedQuantity = selected.Sum(x => x.QuantitySelected);
        return new FifoBatchSelectionResponse(
            medicine.Id,
            medicine.BrandName,
            quantityRequired,
            selectedQuantity,
            selectedQuantity == quantityRequired,
            selected);
    }

    private static Result<StockAdjustment> ApplyAdjustment(
        StockBatch batch,
        int quantityDelta,
        StockAdjustmentType adjustmentType,
        string reason)
    {
        var previousQuantity = batch.QuantityOnHand;
        var newQuantity = previousQuantity + quantityDelta;
        if (newQuantity < 0)
        {
            return Result<StockAdjustment>.Failure(new Error("STOCK_QUANTITY_INVALID", "Stock adjustment cannot reduce quantity below zero."));
        }

        batch.QuantityOnHand = newQuantity;
        return Result<StockAdjustment>.Success(new StockAdjustment
        {
            TenantId = batch.TenantId,
            MedicineId = batch.MedicineId,
            Medicine = batch.Medicine,
            StockBatchId = batch.Id,
            StockBatch = batch,
            AdjustmentType = adjustmentType,
            QuantityDelta = quantityDelta,
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
            Reason = reason.Trim(),
            AdjustedAt = DateTimeOffset.UtcNow
        });
    }

    private async Task ResolveLowStockIfRecoveredAsync(Guid medicineId, CancellationToken cancellationToken)
    {
        var medicine = await MedicineQuery().SingleAsync(x => x.Id == medicineId, cancellationToken);
        var stockOnHand = medicine.StockBatches.Sum(x => x.QuantityOnHand);
        if (stockOnHand <= medicine.ReorderLevel)
        {
            return;
        }

        var openAlerts = await dbContext.StockAlerts
            .Where(x => x.MedicineId == medicineId
                && x.AlertType == StockAlertType.LowStock
                && x.Status == StockAlertStatus.Open)
            .ToListAsync(cancellationToken);

        foreach (var alert in openAlerts)
        {
            alert.Status = StockAlertStatus.Resolved;
            alert.ResolvedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task<StockAlert?> EnsureAlertAsync(
        Medicine medicine,
        StockBatch? stockBatch,
        StockAlertType alertType,
        string severity,
        string message,
        int? quantityOnHand,
        int? thresholdQuantity,
        DateOnly? expiryDate,
        DateTimeOffset detectedAt,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.StockAlerts.AnyAsync(x =>
            x.MedicineId == medicine.Id
            && x.StockBatchId == (stockBatch == null ? null : stockBatch.Id)
            && x.AlertType == alertType
            && x.Status == StockAlertStatus.Open,
            cancellationToken);
        if (exists)
        {
            return null;
        }

        var alert = new StockAlert
        {
            TenantId = medicine.TenantId,
            MedicineId = medicine.Id,
            Medicine = medicine,
            StockBatchId = stockBatch?.Id,
            StockBatch = stockBatch,
            AlertType = alertType,
            Status = StockAlertStatus.Open,
            Severity = severity,
            Message = message,
            QuantityOnHand = quantityOnHand,
            ThresholdQuantity = thresholdQuantity,
            ExpiryDate = expiryDate,
            DetectedAt = detectedAt
        };

        dbContext.StockAlerts.Add(alert);
        return alert;
    }

    private static StockAlertType? ResolveExpiryAlertType(DateOnly today, DateOnly expiryDate)
    {
        var days = (expiryDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;
        return days switch
        {
            < 0 => null,
            <= 30 => StockAlertType.Expiry30Days,
            <= 60 => StockAlertType.Expiry60Days,
            <= 90 => StockAlertType.Expiry90Days,
            _ => null
        };
    }

    private async Task<string> GenerateDispenseNumberAsync(DateTimeOffset dispensedAt, CancellationToken cancellationToken)
    {
        var datePart = dispensedAt.UtcDateTime.ToString("yyyyMMdd");
        var prefix = $"DSP-{datePart}-";
        var count = await dbContext.PrescriptionDispenses.CountAsync(x => x.DispenseNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private async Task<string> GenerateOrderNumberAsync(DateTimeOffset orderedAt, CancellationToken cancellationToken)
    {
        var datePart = orderedAt.UtcDateTime.ToString("yyyyMMdd");
        var prefix = $"PHO-{datePart}-";
        var count = await dbContext.PharmacyOrders.CountAsync(x => x.OrderNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private static byte[] GenerateReceiptPdf(PrescriptionDispense dispense)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(column =>
                {
                    column.Item().Text("HealthCareMS Pharmacy Receipt").FontSize(18).Bold().FontColor(Colors.Green.Darken2);
                    column.Item().Text($"Receipt: {dispense.ReceiptNumber}");
                    column.Item().Text($"Prescription: {dispense.Prescription.PrescriptionNumber}");
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Patient").Bold();
                            left.Item().Text($"{dispense.Patient.FirstName} {dispense.Patient.LastName}".Trim());
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Doctor").Bold();
                            right.Item().Text(dispense.Doctor.User.FullName);
                            right.Item().Text($"PMDC: {dispense.Doctor.PmdcRegistrationNumber}");
                        });
                    });

                    column.Item().Text($"Dispensed at {dispense.DispensedAt:yyyy-MM-dd HH:mm} UTC");
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Medicine");
                            header.Cell().Element(HeaderCell).Text("Qty");
                            header.Cell().Element(HeaderCell).Text("Unit");
                            header.Cell().Element(HeaderCell).Text("Total");
                            header.Cell().Element(HeaderCell).Text("Batches");
                        });

                        foreach (var item in dispense.Items.OrderBy(x => x.DispensedMedicineName))
                        {
                            var batchText = string.Join(", ", item.Batches
                                .OrderBy(x => x.BatchNumber)
                                .Select(x => $"{x.BatchNumber} x{x.QuantityDispensed}"));
                            table.Cell().Element(BodyCell).Text(item.DispensedMedicineName);
                            table.Cell().Element(BodyCell).Text(item.QuantityDispensed.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Element(BodyCell).Text(item.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture));
                            table.Cell().Element(BodyCell).Text(item.LineTotal.ToString("0.00", CultureInfo.InvariantCulture));
                            table.Cell().Element(BodyCell).Text(batchText);
                        }
                    });

                    column.Item().AlignRight().Column(total =>
                    {
                        total.Item().Text($"SubTotal: {dispense.SubTotal:0.00}").Bold();
                        total.Item().Text($"Total: {dispense.TotalAmount:0.00}").FontSize(13).Bold();
                    });

                    if (!string.IsNullOrWhiteSpace(dispense.Notes))
                    {
                        column.Item().Text("Notes").Bold();
                        column.Item().Text(dispense.Notes);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Verification code: ");
                    text.Span(dispense.VerificationCode).Bold();
                });
            });
        }).GeneratePdf();
    }

    private static MedicineResponse Map(Medicine medicine)
    {
        var batches = medicine.StockBatches
            .OrderBy(x => x.ExpiryDate)
            .Select(Map)
            .ToList();

        return new MedicineResponse(
            medicine.Id,
            medicine.TenantId,
            medicine.GenericName,
            medicine.BrandName,
            medicine.DosageForm,
            medicine.Strength,
            medicine.DrapRegistrationNumber,
            medicine.Manufacturer,
            medicine.UnitPrice,
            medicine.UnitCostPrice,
            medicine.IsControlled,
            medicine.ReorderLevel,
            medicine.Barcode,
            medicine.IsActive,
            batches.Sum(x => x.QuantityOnHand),
            medicine.CreatedAt,
            batches);
    }

    private static StockBatchResponse Map(StockBatch batch)
    {
        return new StockBatchResponse(
            batch.Id,
            batch.MedicineId,
            batch.SupplierId,
            batch.BatchNumber,
            batch.ManufacturedDate,
            batch.ExpiryDate,
            batch.QuantityOnHand,
            batch.UnitCostPrice,
            batch.ReceivedAt);
    }

    private static StockAdjustmentResponse Map(StockAdjustment adjustment)
    {
        return new StockAdjustmentResponse(
            adjustment.Id,
            adjustment.MedicineId,
            adjustment.StockBatchId,
            adjustment.StockBatch.BatchNumber,
            adjustment.AdjustmentType.ToString(),
            adjustment.QuantityDelta,
            adjustment.PreviousQuantity,
            adjustment.NewQuantity,
            adjustment.Reason,
            adjustment.AdjustedAt);
    }

    private static StockAlertResponse Map(StockAlert alert)
    {
        return new StockAlertResponse(
            alert.Id,
            alert.MedicineId,
            alert.Medicine.BrandName,
            alert.StockBatchId,
            alert.StockBatch?.BatchNumber,
            alert.AlertType.ToString(),
            alert.Status.ToString(),
            alert.Severity,
            alert.Message,
            alert.ThresholdQuantity,
            alert.QuantityOnHand,
            alert.ExpiryDate,
            alert.DetectedAt);
    }

    private static PrescriptionDispenseResponse Map(PrescriptionDispense dispense)
    {
        return new PrescriptionDispenseResponse(
            dispense.Id,
            dispense.DispenseNumber,
            dispense.ReceiptNumber,
            dispense.PrescriptionId,
            dispense.Prescription.PrescriptionNumber,
            dispense.PatientId,
            $"{dispense.Patient.FirstName} {dispense.Patient.LastName}".Trim(),
            dispense.DoctorId,
            dispense.Doctor.User.FullName,
            dispense.Status.ToString(),
            dispense.DispensedAt,
            dispense.SubTotal,
            dispense.TotalAmount,
            dispense.Notes,
            dispense.Items
                .OrderBy(x => x.DispensedMedicineName)
                .Select(Map)
                .ToList());
    }

    private static PrescriptionDispenseItemResponse Map(PrescriptionDispenseItem item)
    {
        return new PrescriptionDispenseItemResponse(
            item.Id,
            item.PrescriptionItemId,
            item.MedicineId,
            item.PrescribedMedicineName,
            item.DispensedMedicineName,
            item.QuantityPrescribed,
            item.QuantityDispensed,
            item.UnitPrice,
            item.LineTotal,
            item.Batches
                .OrderBy(x => x.BatchNumber)
                .Select(Map)
                .ToList());
    }

    private static PrescriptionDispenseBatchResponse Map(PrescriptionDispenseBatch batch)
    {
        return new PrescriptionDispenseBatchResponse(
            batch.StockBatchId,
            batch.BatchNumber,
            batch.StockBatch.ExpiryDate,
            batch.QuantityDispensed);
    }

    private static PharmacyOrderResponse Map(PharmacyOrder order)
    {
        return new PharmacyOrderResponse(
            order.Id,
            order.TenantId,
            order.OrderNumber,
            order.PatientId,
            $"{order.Patient.FirstName} {order.Patient.LastName}".Trim(),
            order.PrescriptionId,
            order.Status.ToString(),
            order.OrderedAt,
            order.ReviewedAt,
            order.ConfirmedAt,
            order.DeliveryAgentUserId,
            order.DeliveryAgentUser?.FullName,
            order.AssignedAt,
            order.DispatchedAt,
            order.DeliveredAt,
            order.DeliveryAddress,
            order.DeliveryWindowStart,
            order.DeliveryWindowEnd,
            order.PrescriptionUploadContent is { Length: > 0 },
            order.PrescriptionUploadFileName,
            order.PatientNotes,
            order.PharmacistNotes,
            order.SubTotal,
            order.DeliveryFee,
            order.TotalAmount,
            order.Items
                .OrderBy(x => x.MedicineName)
                .Select(Map)
                .ToList());
    }

    private static PharmacyOrderItemResponse Map(PharmacyOrderItem item)
    {
        return new PharmacyOrderItemResponse(
            item.Id,
            item.MedicineId,
            item.MedicineName,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal);
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Green.Lighten4)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(5);
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5);
    }

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Add('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(new string(current.ToArray()).Trim());
                current.Clear();
            }
            else
            {
                current.Add(ch);
            }
        }

        values.Add(new string(current.ToArray()).Trim());
        return values;
    }

    private static string GenerateBarcode(string brandName, string registrationNumber)
    {
        _ = brandName;
        _ = registrationNumber;
        return $"MED-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
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
