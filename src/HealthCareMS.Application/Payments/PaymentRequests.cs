namespace HealthCareMS.Application.Payments;

public sealed record CreateOrderPaymentRequest(
    string Gateway,
    string? ReturnUrl,
    string? CancelUrl,
    string? CustomerIpAddress);

public sealed record PaymentWebhookRequest(
    string SessionToken,
    string EventType,
    string Status,
    decimal Amount,
    string? Currency,
    string? ExternalReference,
    string? FailureCode,
    string? FailureMessage,
    string? PayloadJson);

public sealed record SimulatePaymentCallbackRequest(
    string Status,
    string? ExternalReference);

public sealed record RefundPaymentRequest(
    decimal? Amount,
    string Reason);
