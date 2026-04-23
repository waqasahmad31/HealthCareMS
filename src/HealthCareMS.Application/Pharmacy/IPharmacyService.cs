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
}
