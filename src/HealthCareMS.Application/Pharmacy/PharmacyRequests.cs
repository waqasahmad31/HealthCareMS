namespace HealthCareMS.Application.Pharmacy;

public sealed record CreateMedicineRequest(
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
    string? Barcode);

public sealed record UpdateMedicineRequest(
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
    string? Barcode,
    bool IsActive);

public sealed record CreateStockBatchRequest(
    Guid? TenantId,
    Guid? SupplierId,
    string BatchNumber,
    DateOnly? ManufacturedDate,
    DateOnly ExpiryDate,
    int QuantityOnHand,
    decimal UnitCostPrice);

public sealed record ImportMedicineCsvRequest(Guid? TenantId, string CsvContent);

public sealed record AdjustStockBatchRequest(
    int QuantityDelta,
    string AdjustmentType,
    string Reason);

public sealed record FifoDispenseRequest(
    int QuantityRequired,
    string? Reason);
