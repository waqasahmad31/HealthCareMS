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

public sealed record DispensePrescriptionLookupResponse(
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
    IReadOnlyList<DispensePrescriptionLookupItemResponse> Items);

public sealed record DispensePrescriptionLookupItemResponse(
    Guid PrescriptionItemId,
    string MedicineName,
    string? GenericName,
    string? Strength,
    string Dosage,
    string Frequency,
    short DurationDays,
    decimal QuantityPrescribed,
    IReadOnlyList<MedicineAvailabilityResponse> SuggestedMedicines);

public sealed record MedicineAvailabilityResponse(
    Guid MedicineId,
    string MedicineName,
    string? Strength,
    decimal UnitPrice,
    int StockOnHand,
    bool IsAvailable);

public sealed record PrescriptionDispenseBatchResponse(
    Guid StockBatchId,
    string BatchNumber,
    DateOnly ExpiryDate,
    int QuantityDispensed);

public sealed record PrescriptionDispenseItemResponse(
    Guid Id,
    Guid PrescriptionItemId,
    Guid MedicineId,
    string PrescribedMedicineName,
    string DispensedMedicineName,
    decimal QuantityPrescribed,
    int QuantityDispensed,
    decimal UnitPrice,
    decimal LineTotal,
    IReadOnlyList<PrescriptionDispenseBatchResponse> Batches);

public sealed record PrescriptionDispenseResponse(
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
    IReadOnlyList<PrescriptionDispenseItemResponse> Items);

public sealed record DispenseReceiptPdfResponse(
    byte[] Content,
    string FileName,
    string ContentType);

public sealed record PharmacyOrderItemResponse(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record PharmacyOrderResponse(
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
    IReadOnlyList<PharmacyOrderItemResponse> Items);
