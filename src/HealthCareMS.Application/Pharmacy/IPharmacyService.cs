using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Pharmacy;

public interface IPharmacyService
{
    Task<IReadOnlyList<MedicineResponse>> SearchMedicinesAsync(string? search, CancellationToken cancellationToken);

    Task<Result<MedicineResponse>> GetMedicineAsync(Guid medicineId, CancellationToken cancellationToken);

    Task<Result<MedicineResponse>> CreateMedicineAsync(CreateMedicineRequest request, CancellationToken cancellationToken);

    Task<Result<MedicineResponse>> UpdateMedicineAsync(
        Guid medicineId,
        UpdateMedicineRequest request,
        CancellationToken cancellationToken);

    Task<Result<StockBatchResponse>> CreateStockBatchAsync(
        Guid medicineId,
        CreateStockBatchRequest request,
        CancellationToken cancellationToken);

    Task<Result<MedicineImportResponse>> ImportMedicinesCsvAsync(
        ImportMedicineCsvRequest request,
        CancellationToken cancellationToken);

    Task<Result<PharmacyStockDashboardResponse>> GetStockDashboardAsync(CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<StockBatchResponse>>> GetStockBatchesAsync(
        Guid medicineId,
        CancellationToken cancellationToken);

    Task<Result<StockAdjustmentResponse>> AdjustStockBatchAsync(
        Guid stockBatchId,
        AdjustStockBatchRequest request,
        CancellationToken cancellationToken);

    Task<Result<FifoBatchSelectionResponse>> GetFifoBatchSelectionAsync(
        Guid medicineId,
        int quantityRequired,
        CancellationToken cancellationToken);

    Task<Result<FifoDispenseResponse>> DispenseFifoAsync(
        Guid medicineId,
        FifoDispenseRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<StockAlertResponse>>> RunStockAlertScanAsync(CancellationToken cancellationToken);

    Task<Result<DispensePrescriptionLookupResponse>> GetPrescriptionForDispensingAsync(
        Guid prescriptionId,
        string verificationCode,
        CancellationToken cancellationToken);

    Task<Result<PrescriptionDispenseResponse>> DispensePrescriptionAsync(
        Guid prescriptionId,
        DispensePrescriptionRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PrescriptionDispenseResponse>>> GetDispensingHistoryAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<Result<DispenseReceiptPdfResponse>> GenerateDispenseReceiptPdfAsync(
        Guid dispenseId,
        CancellationToken cancellationToken);

    Task<Result<PharmacyOrderResponse>> CreateOrderAsync(
        CreatePharmacyOrderRequest request,
        CancellationToken cancellationToken);

    Task<Result<PharmacyOrderResponse>> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PharmacyOrderResponse>>> GetOrdersAsync(
        string? status,
        Guid? patientId,
        Guid? deliveryAgentUserId,
        CancellationToken cancellationToken);

    Task<Result<PharmacyOrderResponse>> ConfirmOrderAsync(
        Guid orderId,
        ConfirmPharmacyOrderRequest request,
        CancellationToken cancellationToken);

    Task<Result<PharmacyOrderResponse>> AssignDeliveryAgentAsync(
        Guid orderId,
        AssignDeliveryAgentRequest request,
        CancellationToken cancellationToken);

    Task<Result<PharmacyOrderResponse>> UpdateOrderStatusAsync(
        Guid orderId,
        UpdatePharmacyOrderStatusRequest request,
        CancellationToken cancellationToken);
}
