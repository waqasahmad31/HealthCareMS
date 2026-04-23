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

public sealed record DispensePrescriptionItemRequest(
    Guid PrescriptionItemId,
    Guid MedicineId,
    int QuantityToDispense);

public sealed record DispensePrescriptionRequest(
    string VerificationCode,
    IReadOnlyList<DispensePrescriptionItemRequest> Items,
    string? Notes);

public sealed record CreatePharmacyOrderItemRequest(
    Guid MedicineId,
    int Quantity);

public sealed record CreatePharmacyOrderRequest(
    Guid? TenantId,
    Guid PatientId,
    Guid? PrescriptionId,
    string DeliveryAddress,
    DateTimeOffset? DeliveryWindowStart,
    DateTimeOffset? DeliveryWindowEnd,
    string? PatientNotes,
    string? PrescriptionUploadFileName,
    string? PrescriptionUploadContentType,
    byte[]? PrescriptionUploadContent,
    IReadOnlyList<CreatePharmacyOrderItemRequest> Items);

public sealed record ConfirmPharmacyOrderRequest(
    Guid? DeliveryAgentUserId,
    string? PharmacistNotes);

public sealed record AssignDeliveryAgentRequest(
    Guid DeliveryAgentUserId);

public sealed record UpdatePharmacyOrderStatusRequest(
    string Status,
    string? Notes);
