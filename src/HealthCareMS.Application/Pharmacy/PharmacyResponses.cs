namespace HealthCareMS.Application.Pharmacy;

public sealed record MedicineResponse(
    Guid Id,
    Guid? TenantId,
    string GenericName,
    string BrandName,
    string DosageForm,
    string? Strength,
    string DrapRegistrationNumber,
    string? Manufacturer,
    decimal UnitPrice,
    decimal UnitCostPrice,
    bool IsControlled,
    int ReorderLevel,
    string Barcode,
    bool IsActive,
    int StockOnHand,
    DateTimeOffset CreatedAt,
    IReadOnlyList<StockBatchResponse> StockBatches);

public sealed record StockBatchResponse(
    Guid Id,
    Guid MedicineId,
    Guid? SupplierId,
    string BatchNumber,
    DateOnly? ManufacturedDate,
    DateOnly ExpiryDate,
    int QuantityOnHand,
    decimal UnitCostPrice,
    DateTimeOffset ReceivedAt);

public sealed record MedicineImportResponse(
    int ImportedCount,
    IReadOnlyList<MedicineResponse> Medicines);

public sealed record StockAdjustmentResponse(
    Guid Id,
    Guid MedicineId,
    Guid StockBatchId,
    string BatchNumber,
    string AdjustmentType,
    int QuantityDelta,
    int PreviousQuantity,
    int NewQuantity,
    string Reason,
    DateTimeOffset AdjustedAt);

public sealed record StockAlertResponse(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    Guid? StockBatchId,
    string? BatchNumber,
    string AlertType,
    string Status,
    string Severity,
    string Message,
    int? ThresholdQuantity,
    int? QuantityOnHand,
    DateOnly? ExpiryDate,
    DateTimeOffset DetectedAt);

public sealed record FifoBatchSelectionItemResponse(
    Guid StockBatchId,
    string BatchNumber,
    DateTimeOffset ReceivedAt,
    DateOnly ExpiryDate,
    int QuantityAvailable,
    int QuantitySelected);

public sealed record FifoBatchSelectionResponse(
    Guid MedicineId,
    string MedicineName,
    int QuantityRequired,
    int QuantitySelected,
    bool IsFulfillable,
    IReadOnlyList<FifoBatchSelectionItemResponse> Batches);

public sealed record FifoDispenseResponse(
    Guid MedicineId,
    string MedicineName,
    int QuantityRequired,
    int QuantityDispensed,
    IReadOnlyList<FifoBatchSelectionItemResponse> Batches,
    IReadOnlyList<StockAdjustmentResponse> Adjustments);

public sealed record PharmacyStockDashboardResponse(
    int TotalMedicines,
    int LowStockCount,
    int ExpiryAlertCount,
    IReadOnlyList<MedicineResponse> LowStockMedicines,
    IReadOnlyList<StockAlertResponse> ExpiryAlerts,
    IReadOnlyList<StockAlertResponse> OpenAlerts);
