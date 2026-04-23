using HealthCareMS.API.Security;
using HealthCareMS.Application.Labs;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/lab")]
public sealed class LabController(ILabService labService) : ApiControllerBase
{
    [HttpGet("tests")]
    [RequirePermission(PermissionKeys.Lab.TestsView)]
    public async Task<IActionResult> SearchTests([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var results = await labService.SearchTestsAsync(search, cancellationToken);
        return OkEnvelope(results);
    }

    [HttpPost("tests/import")]
    [RequirePermission(PermissionKeys.Lab.ResultsEntry)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> ImportTests(
        [FromForm] IFormFile file,
        [FromForm] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Fail(new Error("LAB_TEST_CSV_REQUIRED", "CSV file is required."));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var csv = await reader.ReadToEndAsync(cancellationToken);
        var result = await labService.ImportTestsCsvAsync(new ImportLabTestsCsvRequest(tenantId, csv), cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("panels")]
    [RequirePermission(PermissionKeys.Lab.TestsView)]
    public async Task<IActionResult> GetPanels([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await labService.GetPanelsAsync(search, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("panels")]
    [RequirePermission(PermissionKeys.Lab.ResultsEntry)]
    public async Task<IActionResult> CreatePanel(CreateLabPanelRequest request, CancellationToken cancellationToken)
    {
        var result = await labService.CreatePanelAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPost("bookings")]
    [Authorize]
    public async Task<IActionResult> CreateBooking(CreateLabBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await labService.CreateBookingAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("bookings")]
    [RequirePermission(PermissionKeys.Lab.SampleCollect)]
    public async Task<IActionResult> GetBookings(
        [FromQuery] string? status,
        [FromQuery] string? collectionType,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var result = await labService.GetBookingsAsync(status, collectionType, date, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("bookings/{bookingId:guid}/check-in")]
    [RequirePermission(PermissionKeys.Lab.SampleCollect)]
    public async Task<IActionResult> CheckInBooking(
        Guid bookingId,
        CheckInLabBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.CheckInBookingAsync(bookingId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("bookings/{bookingId:guid}/barcode-label.pdf")]
    [RequirePermission(PermissionKeys.Lab.SampleCollect)]
    public async Task<IActionResult> DownloadBarcodeLabel(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await labService.GenerateBarcodeLabelPdfAsync(bookingId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("appointments/{appointmentId:guid}/bookings")]
    [RequirePermission(PermissionKeys.Lab.TestsView)]
    public async Task<IActionResult> GetBookingsByAppointment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await labService.GetBookingsByAppointmentAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }
}
