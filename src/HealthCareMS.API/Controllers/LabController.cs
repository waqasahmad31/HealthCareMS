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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportTests(
        [FromForm] ImportLabTestsForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return Fail(new Error("LAB_TEST_CSV_REQUIRED", "CSV file is required."));
        }

        using var reader = new StreamReader(form.File.OpenReadStream());
        var csv = await reader.ReadToEndAsync(cancellationToken);
        var result = await labService.ImportTestsCsvAsync(new ImportLabTestsCsvRequest(form.TenantId, csv), cancellationToken);
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

    [HttpPut("bookings/{bookingId:guid}/collection-agent")]
    [RequirePermission(PermissionKeys.Lab.SampleCollect)]
    public async Task<IActionResult> AssignCollectionAgent(
        Guid bookingId,
        AssignLabCollectionAgentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.AssignCollectionAgentAsync(bookingId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("collections/assigned/{collectionAgentUserId:guid}")]
    [RequirePermission(PermissionKeys.Lab.SampleCollect)]
    public async Task<IActionResult> GetAssignedCollections(
        Guid collectionAgentUserId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await labService.GetAssignedCollectionsAsync(collectionAgentUserId, status, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("bookings/{bookingId:guid}/collection/start")]
    [RequirePermission(PermissionKeys.Lab.SampleCollect)]
    public async Task<IActionResult> StartCollection(
        Guid bookingId,
        StartLabCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.StartCollectionAsync(bookingId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("bookings/{bookingId:guid}/collection/collect")]
    [RequirePermission(PermissionKeys.Lab.SampleCollect)]
    public async Task<IActionResult> MarkSampleCollected(
        Guid bookingId,
        MarkLabSampleCollectedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.MarkSampleCollectedAsync(bookingId, request, cancellationToken);
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

    [HttpPut("bookings/{bookingId:guid}/results")]
    [RequirePermission(PermissionKeys.Lab.ResultsEntry)]
    public async Task<IActionResult> EnterResults(
        Guid bookingId,
        EnterLabResultsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.EnterResultsAsync(bookingId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("bookings/{bookingId:guid}/results")]
    [RequirePermission(PermissionKeys.Lab.ResultsEntry)]
    public async Task<IActionResult> GetResults(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await labService.GetResultsAsync(bookingId, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("results/{resultId:guid}/critical-alert/acknowledge")]
    [RequirePermission(PermissionKeys.Lab.ResultsValidate)]
    public async Task<IActionResult> AcknowledgeCriticalAlert(
        Guid resultId,
        AcknowledgeLabCriticalAlertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.AcknowledgeCriticalAlertAsync(resultId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("results/validation-queue")]
    [RequirePermission(PermissionKeys.Lab.ResultsValidate)]
    public async Task<IActionResult> GetValidationQueue([FromQuery] string? filter, CancellationToken cancellationToken)
    {
        var result = await labService.GetValidationQueueAsync(filter, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("bookings/{bookingId:guid}/results/validate")]
    [RequirePermission(PermissionKeys.Lab.ResultsValidate)]
    public async Task<IActionResult> ValidateResults(
        Guid bookingId,
        ValidateLabResultsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.ValidateResultsAsync(bookingId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("bookings/{bookingId:guid}/results/release")]
    [RequirePermission(PermissionKeys.Lab.ResultsRelease)]
    public async Task<IActionResult> ReleaseResults(
        Guid bookingId,
        ReleaseLabResultsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.ReleaseResultsAsync(bookingId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("results/{resultId:guid}/addendum")]
    [RequirePermission(PermissionKeys.Lab.ResultsRelease)]
    public async Task<IActionResult> AddAddendum(
        Guid resultId,
        AddLabResultAddendumRequest request,
        CancellationToken cancellationToken)
    {
        var result = await labService.AddAddendumAsync(resultId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("bookings/{bookingId:guid}/report.pdf")]
    [RequirePermission(PermissionKeys.Lab.ReportsDownload)]
    public async Task<IActionResult> DownloadLabReport(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await labService.GenerateReportPdfAsync(bookingId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("patients/{patientId:guid}/results")]
    [Authorize]
    public async Task<IActionResult> GetPatientResults(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await labService.GetPatientResultsAsync(patientId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("reports/verify/{bookingId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyReport(
        Guid bookingId,
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        var result = await labService.VerifyReportAsync(bookingId, code, cancellationToken);
        return FromResult(result);
    }

    public sealed class ImportLabTestsForm
    {
        public IFormFile? File { get; set; }

        public Guid? TenantId { get; set; }
    }
}
