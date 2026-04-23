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
}
