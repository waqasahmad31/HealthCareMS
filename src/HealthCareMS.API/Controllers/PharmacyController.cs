using HealthCareMS.API.Security;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/pharmacy")]
public sealed class PharmacyController(IPharmacyService pharmacyService) : ApiControllerBase
{
    [HttpGet("medicines")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupGetMedium")]
    public async Task<IActionResult> SearchMedicines([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var results = await pharmacyService.SearchMedicinesAsync(search, cancellationToken);
        return OkEnvelope(results);
    }

    [HttpGet("medicines/{medicineId:guid}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupGetMedium")]
    public async Task<IActionResult> GetMedicine(Guid medicineId, CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetMedicineAsync(medicineId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("stock/dashboard")]
    [RequirePermission(PermissionKeys.Pharmacy.StockView)]
    public async Task<IActionResult> GetStockDashboard(CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetStockDashboardAsync(cancellationToken);
        return FromResult(result);
    }

    [HttpPost("stock/alerts/scan")]
    [RequirePermission(PermissionKeys.Pharmacy.StockAdjust)]
    public async Task<IActionResult> RunStockAlertScan(CancellationToken cancellationToken)
    {
        var result = await pharmacyService.RunStockAlertScanAsync(cancellationToken);
        return FromResult(result);
    }

    [HttpPost("medicines")]
    [RequirePermission(PermissionKeys.Pharmacy.MedicinesCreate)]
    public async Task<IActionResult> CreateMedicine(CreateMedicineRequest request, CancellationToken cancellationToken)
    {
        var result = await pharmacyService.CreateMedicineAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("medicines/{medicineId:guid}")]
    [RequirePermission(PermissionKeys.Pharmacy.MedicinesEdit)]
    public async Task<IActionResult> UpdateMedicine(
        Guid medicineId,
        UpdateMedicineRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.UpdateMedicineAsync(medicineId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("medicines/{medicineId:guid}/stock-batches")]
    [RequirePermission(PermissionKeys.Pharmacy.StockAdjust)]
    public async Task<IActionResult> CreateStockBatch(
        Guid medicineId,
        CreateStockBatchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.CreateStockBatchAsync(medicineId, request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("medicines/{medicineId:guid}/stock-batches")]
    [RequirePermission(PermissionKeys.Pharmacy.StockView)]
    public async Task<IActionResult> GetStockBatches(Guid medicineId, CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetStockBatchesAsync(medicineId, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("stock-batches/{stockBatchId:guid}/adjustments")]
    [RequirePermission(PermissionKeys.Pharmacy.StockAdjust)]
    public async Task<IActionResult> AdjustStockBatch(
        Guid stockBatchId,
        AdjustStockBatchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.AdjustStockBatchAsync(stockBatchId, request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("medicines/{medicineId:guid}/fifo-selection")]
    [RequirePermission(PermissionKeys.Pharmacy.StockView)]
    public async Task<IActionResult> GetFifoSelection(
        Guid medicineId,
        [FromQuery] int quantityRequired,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetFifoBatchSelectionAsync(medicineId, quantityRequired, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("medicines/{medicineId:guid}/fifo-dispense")]
    [RequirePermission(PermissionKeys.Pharmacy.Dispense)]
    public async Task<IActionResult> DispenseFifo(
        Guid medicineId,
        FifoDispenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.DispenseFifoAsync(medicineId, request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("dispense/prescriptions/{prescriptionId:guid}")]
    [RequirePermission(PermissionKeys.Pharmacy.Dispense)]
    public async Task<IActionResult> GetPrescriptionForDispensing(
        Guid prescriptionId,
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetPrescriptionForDispensingAsync(prescriptionId, code, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("dispense/prescriptions/{prescriptionId:guid}")]
    [RequirePermission(PermissionKeys.Pharmacy.Dispense)]
    public async Task<IActionResult> DispensePrescription(
        Guid prescriptionId,
        DispensePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.DispensePrescriptionAsync(prescriptionId, request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("dispense/history")]
    [RequirePermission(PermissionKeys.Pharmacy.OrdersView)]
    public async Task<IActionResult> GetDispensingHistory([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetDispensingHistoryAsync(search, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("dispense/{dispenseId:guid}/receipt.pdf")]
    [RequirePermission(PermissionKeys.Pharmacy.OrdersView)]
    public async Task<IActionResult> DownloadReceipt(Guid dispenseId, CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GenerateDispenseReceiptPdfAsync(dispenseId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("orders")]
    [Authorize]
    public async Task<IActionResult> CreateOrder(CreatePharmacyOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await pharmacyService.CreateOrderAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("orders")]
    [RequirePermission(PermissionKeys.Pharmacy.OrdersView)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? deliveryAgentUserId,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetOrdersAsync(status, patientId, deliveryAgentUserId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("orders/{orderId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetOrderAsync(orderId, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("orders/{orderId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Pharmacy.OrdersProcess)]
    public async Task<IActionResult> ConfirmOrder(
        Guid orderId,
        ConfirmPharmacyOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.ConfirmOrderAsync(orderId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("orders/{orderId:guid}/delivery-agent")]
    [RequirePermission(PermissionKeys.Pharmacy.OrdersProcess)]
    public async Task<IActionResult> AssignDeliveryAgent(
        Guid orderId,
        AssignDeliveryAgentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.AssignDeliveryAgentAsync(orderId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("orders/{orderId:guid}/status")]
    [RequirePermission(PermissionKeys.Pharmacy.OrdersProcess)]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid orderId,
        UpdatePharmacyOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.UpdateOrderStatusAsync(orderId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("reports")]
    [RequirePermission(PermissionKeys.Pharmacy.ReportsView)]
    public async Task<IActionResult> GetReports(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetReportsAsync(from, to, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("reports/export")]
    [RequirePermission(PermissionKeys.Pharmacy.ReportsView)]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string reportType,
        [FromQuery] string format = "pdf",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await pharmacyService.ExportReportAsync(reportType, from, to, format, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("medicines/import")]
    [RequirePermission(PermissionKeys.Pharmacy.MedicinesCreate)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportMedicines(
        [FromForm] ImportMedicinesForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return Fail(new Error("MEDICINE_CSV_REQUIRED", "CSV file is required."));
        }

        using var reader = new StreamReader(form.File.OpenReadStream());
        var csv = await reader.ReadToEndAsync(cancellationToken);
        var result = await pharmacyService.ImportMedicinesCsvAsync(new ImportMedicineCsvRequest(form.TenantId, csv), cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    public sealed class ImportMedicinesForm
    {
        public IFormFile? File { get; set; }

        public Guid? TenantId { get; set; }
    }
}
