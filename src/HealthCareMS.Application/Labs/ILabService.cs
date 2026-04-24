using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Labs;

public interface ILabService
{
    Task<IReadOnlyList<LabTestResponse>> SearchTestsAsync(string? search, CancellationToken cancellationToken);

    Task<Result<LabTestImportResponse>> ImportTestsCsvAsync(
        ImportLabTestsCsvRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabPanelResponse>>> GetPanelsAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<Result<LabPanelResponse>> CreatePanelAsync(
        CreateLabPanelRequest request,
        CancellationToken cancellationToken);

    Task<Result<LabBookingResponse>> CreateBookingAsync(
        CreateLabBookingRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabBookingResponse>>> GetBookingsAsync(
        string? status,
        string? collectionType,
        DateOnly? date,
        CancellationToken cancellationToken);

    Task<Result<LabBookingResponse>> CheckInBookingAsync(
        Guid bookingId,
        CheckInLabBookingRequest request,
        CancellationToken cancellationToken);

    Task<Result<LabBookingResponse>> AssignCollectionAgentAsync(
        Guid bookingId,
        AssignLabCollectionAgentRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabBookingResponse>>> GetAssignedCollectionsAsync(
        Guid collectionAgentUserId,
        string? status,
        CancellationToken cancellationToken);

    Task<Result<LabBookingResponse>> StartCollectionAsync(
        Guid bookingId,
        StartLabCollectionRequest request,
        CancellationToken cancellationToken);

    Task<Result<LabBookingResponse>> MarkSampleCollectedAsync(
        Guid bookingId,
        MarkLabSampleCollectedRequest request,
        CancellationToken cancellationToken);

    Task<Result<LabBarcodeLabelPdfResponse>> GenerateBarcodeLabelPdfAsync(
        Guid bookingId,
        CancellationToken cancellationToken);

    Task<Result<LabBookingResponse>> CreateConsultationLabOrderAsync(
        Guid appointmentId,
        CreateConsultationLabOrderRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabBookingResponse>>> GetBookingsByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabTestResultResponse>>> EnterResultsAsync(
        Guid bookingId,
        EnterLabResultsRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabTestResultResponse>>> GetResultsAsync(
        Guid bookingId,
        CancellationToken cancellationToken);

    Task<Result<LabTestResultResponse>> AcknowledgeCriticalAlertAsync(
        Guid resultId,
        AcknowledgeLabCriticalAlertRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabValidationQueueItemResponse>>> GetValidationQueueAsync(
        string? filter,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabTestResultResponse>>> ValidateResultsAsync(
        Guid bookingId,
        ValidateLabResultsRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabTestResultResponse>>> ReleaseResultsAsync(
        Guid bookingId,
        ReleaseLabResultsRequest request,
        CancellationToken cancellationToken);

    Task<Result<LabTestResultResponse>> AddAddendumAsync(
        Guid resultId,
        AddLabResultAddendumRequest request,
        CancellationToken cancellationToken);

    Task<Result<LabReportPdfResponse>> GenerateReportPdfAsync(
        Guid bookingId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabBookingResultSummaryResponse>>> GetPatientResultsAsync(
        Guid patientId,
        CancellationToken cancellationToken);

    Task<Result<LabReportVerificationResponse>> VerifyReportAsync(
        Guid bookingId,
        string verificationCode,
        CancellationToken cancellationToken);
}
