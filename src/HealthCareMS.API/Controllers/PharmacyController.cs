using HealthCareMS.API.Security;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/pharmacy")]
public sealed class PharmacyController(IPharmacyService pharmacyService) : ApiControllerBase
{
    [HttpGet("medicines")]
    [RequirePermission(PermissionKeys.Pharmacy.MedicinesView)]
    public async Task<IActionResult> SearchMedicines([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var results = await pharmacyService.SearchMedicinesAsync(search, cancellationToken);
        return OkEnvelope(results);
    }

    [HttpGet("medicines/{medicineId:guid}")]
    [RequirePermission(PermissionKeys.Pharmacy.MedicinesView)]
    public async Task<IActionResult> GetMedicine(Guid medicineId, CancellationToken cancellationToken)
    {
        var result = await pharmacyService.GetMedicineAsync(medicineId, cancellationToken);
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

    [HttpPost("medicines/import")]
    [RequirePermission(PermissionKeys.Pharmacy.MedicinesCreate)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> ImportMedicines(
        [FromForm] IFormFile file,
        [FromForm] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Fail(new Error("MEDICINE_CSV_REQUIRED", "CSV file is required."));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var csv = await reader.ReadToEndAsync(cancellationToken);
        var result = await pharmacyService.ImportMedicinesCsvAsync(new ImportMedicineCsvRequest(tenantId, csv), cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }
}
