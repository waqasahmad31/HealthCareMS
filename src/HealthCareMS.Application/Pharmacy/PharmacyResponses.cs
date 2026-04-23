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
