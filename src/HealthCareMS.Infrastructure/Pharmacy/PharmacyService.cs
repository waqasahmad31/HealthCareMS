using System.Globalization;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Domain.Pharmacy;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Pharmacy;

public sealed class PharmacyService(HealthCareDbContext dbContext) : IPharmacyService
{
    public async Task<IReadOnlyList<MedicineResponse>> SearchMedicinesAsync(string? search, CancellationToken cancellationToken)
    {
        var query = MedicineQuery();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.BrandName.ToLower().Contains(term)
                || x.GenericName.ToLower().Contains(term)
                || x.DrapRegistrationNumber.ToLower().Contains(term)
                || x.Barcode.ToLower().Contains(term));
        }

        var medicines = await query
            .OrderBy(x => x.BrandName)
            .Take(100)
            .ToListAsync(cancellationToken);

        return medicines.Select(Map).ToList();
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
