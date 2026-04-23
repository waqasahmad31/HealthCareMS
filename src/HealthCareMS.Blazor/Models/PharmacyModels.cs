namespace HealthCareMS.Blazor.Models;

public class MedicineFormModel
{
    public Guid? TenantId { get; set; }

    public string GenericName { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string DosageForm { get; set; } = "Tablet";

    public string? Strength { get; set; }

    public string DrapRegistrationNumber { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal UnitCostPrice { get; set; }

    public bool IsControlled { get; set; }

    public int ReorderLevel { get; set; } = 10;

    public string? Barcode { get; set; }
}

public sealed class UpdateMedicineFormModel : MedicineFormModel
{
    public bool IsActive { get; set; } = true;
}

public sealed class StockBatchFormModel
{
    public Guid? TenantId { get; set; }

    public Guid? SupplierId { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly? ManufacturedDate { get; set; }

    public DateOnly ExpiryDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(12));

    public int QuantityOnHand { get; set; }

    public decimal UnitCostPrice { get; set; }
}

public sealed record MedicineModel(
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
    IReadOnlyList<StockBatchModel> StockBatches);

public sealed record StockBatchModel(
    Guid Id,
    Guid MedicineId,
    Guid? SupplierId,
    string BatchNumber,
    DateOnly? ManufacturedDate,
    DateOnly ExpiryDate,
    int QuantityOnHand,
    decimal UnitCostPrice,
    DateTimeOffset ReceivedAt);

public sealed record MedicineImportModel(
    int ImportedCount,
    IReadOnlyList<MedicineModel> Medicines);

public sealed class StockAdjustmentFormModel
{
    public int QuantityDelta { get; set; }

    public string AdjustmentType { get; set; } = "Correction";

    public string Reason { get; set; } = string.Empty;
}

public sealed class FifoDispenseFormModel
{
    public int QuantityRequired { get; set; } = 1;

    public string? Reason { get; set; }
}

public sealed record StockAdjustmentModel(
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

public sealed record StockAlertModel(
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

public sealed record FifoBatchSelectionItemModel(
    Guid StockBatchId,
    string BatchNumber,
    DateTimeOffset ReceivedAt,
    DateOnly ExpiryDate,
    int QuantityAvailable,
    int QuantitySelected);

public sealed record FifoBatchSelectionModel(
    Guid MedicineId,
    string MedicineName,
    int QuantityRequired,
    int QuantitySelected,
    bool IsFulfillable,
    IReadOnlyList<FifoBatchSelectionItemModel> Batches);

public sealed record FifoDispenseModel(
    Guid MedicineId,
    string MedicineName,
    int QuantityRequired,
    int QuantityDispensed,
    IReadOnlyList<FifoBatchSelectionItemModel> Batches,
    IReadOnlyList<StockAdjustmentModel> Adjustments);

public sealed record PharmacyStockDashboardModel(
    int TotalMedicines,
    int LowStockCount,
    int ExpiryAlertCount,
    IReadOnlyList<MedicineModel> LowStockMedicines,
    IReadOnlyList<StockAlertModel> ExpiryAlerts,
    IReadOnlyList<StockAlertModel> OpenAlerts);

public sealed record DispensePrescriptionLookupModel(
    Guid PrescriptionId,
    string PrescriptionNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string? DoctorPmdcRegistrationNumber,
    bool DoctorLicenseValid,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset ValidUntil,
    bool VerificationCodeMatched,
    bool IsDispensable,
    bool AlreadyDispensed,
    IReadOnlyList<string> ValidationMessages,
    IReadOnlyList<DispensePrescriptionLookupItemModel> Items);

public sealed record DispensePrescriptionLookupItemModel(
    Guid PrescriptionItemId,
    string MedicineName,
    string? GenericName,
    string? Strength,
    string Dosage,
    string Frequency,
    short DurationDays,
    decimal QuantityPrescribed,
    IReadOnlyList<MedicineAvailabilityModel> SuggestedMedicines);

public sealed record MedicineAvailabilityModel(
    Guid MedicineId,
    string MedicineName,
    string? Strength,
    decimal UnitPrice,
    int StockOnHand,
    bool IsAvailable);

public sealed record DispensePrescriptionItemFormModel(
    Guid PrescriptionItemId,
    Guid MedicineId,
    int QuantityToDispense);

public sealed record DispensePrescriptionFormModel(
    string VerificationCode,
    IReadOnlyList<DispensePrescriptionItemFormModel> Items,
    string? Notes);

public sealed record PrescriptionDispenseBatchModel(
    Guid StockBatchId,
    string BatchNumber,
    DateOnly ExpiryDate,
    int QuantityDispensed);

public sealed record PrescriptionDispenseItemModel(
    Guid Id,
    Guid PrescriptionItemId,
    Guid MedicineId,
    string PrescribedMedicineName,
    string DispensedMedicineName,
    decimal QuantityPrescribed,
    int QuantityDispensed,
    decimal UnitPrice,
    decimal LineTotal,
    IReadOnlyList<PrescriptionDispenseBatchModel> Batches);

public sealed record PrescriptionDispenseModel(
    Guid Id,
    string DispenseNumber,
    string ReceiptNumber,
    Guid PrescriptionId,
    string PrescriptionNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string Status,
    DateTimeOffset DispensedAt,
    decimal SubTotal,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyList<PrescriptionDispenseItemModel> Items);

public sealed class PharmacyOrderItemFormModel
{
    public Guid MedicineId { get; set; }

    public int Quantity { get; set; } = 1;
}

public sealed class PharmacyOrderFormModel
{
    public Guid? TenantId { get; set; }

    public Guid PatientId { get; set; }

    public Guid? PrescriptionId { get; set; }

    public string DeliveryAddress { get; set; } = string.Empty;

    public DateTimeOffset? DeliveryWindowStart { get; set; }

    public DateTimeOffset? DeliveryWindowEnd { get; set; }

    public string? PatientNotes { get; set; }

    public string? PrescriptionUploadFileName { get; set; }

    public string? PrescriptionUploadContentType { get; set; }

    public byte[]? PrescriptionUploadContent { get; set; }

    public IReadOnlyList<PharmacyOrderItemFormModel> Items { get; set; } = [];
}

public sealed class ConfirmPharmacyOrderFormModel
{
    public Guid? DeliveryAgentUserId { get; set; }

    public string? PharmacistNotes { get; set; }
}

public sealed class AssignDeliveryAgentFormModel
{
    public Guid DeliveryAgentUserId { get; set; }
}

public sealed class UpdatePharmacyOrderStatusFormModel
{
    public string Status { get; set; } = "Prepared";

    public string? Notes { get; set; }
}

public sealed record PharmacyOrderItemModel(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record PharmacyOrderModel(
    Guid Id,
    Guid? TenantId,
    string OrderNumber,
    Guid PatientId,
    string PatientName,
    Guid? PrescriptionId,
    string Status,
    DateTimeOffset OrderedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? ConfirmedAt,
    Guid? DeliveryAgentUserId,
    string? DeliveryAgentName,
    DateTimeOffset? AssignedAt,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    string DeliveryAddress,
    DateTimeOffset? DeliveryWindowStart,
    DateTimeOffset? DeliveryWindowEnd,
    bool HasPrescriptionUpload,
    string? PrescriptionUploadFileName,
    string? PatientNotes,
    string? PharmacistNotes,
    decimal SubTotal,
    decimal DeliveryFee,
    decimal TotalAmount,
    IReadOnlyList<PharmacyOrderItemModel> Items);
