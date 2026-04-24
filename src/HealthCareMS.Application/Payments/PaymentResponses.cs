namespace HealthCareMS.Application.Payments;

public sealed record PaymentGatewayOptionResponse(
    string Gateway,
    string DisplayName,
    bool IsEnabled,
    string CheckoutBaseUrl);

public sealed record PaymentRefundResponse(
    Guid Id,
    string RefundNumber,
    string Status,
    decimal Amount,
    string Currency,
    string Reason,
    string? ExternalReference,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt);

public sealed record PaymentInvoiceResponse(
    Guid Id,
    Guid? TenantId,
    Guid PaymentTransactionId,
    Guid? PharmacyOrderId,
    string InvoiceNumber,
    string Status,
    string BillingName,
    string? BillingEmail,
    string? BillingPhone,
    string? BillingAddress,
    decimal SubTotal,
    decimal DeliveryFee,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset IssuedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? RefundedAt,
    string? Notes);

public sealed record PaymentTransactionResponse(
    Guid Id,
    Guid? TenantId,
    Guid? PharmacyOrderId,
    string ReferenceType,
    Guid ReferenceId,
    string PaymentNumber,
    string Gateway,
    string Status,
    decimal Amount,
    string Currency,
    string SessionToken,
    string CheckoutUrl,
    string? ExternalReference,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? FailedAt,
    string? FailureCode,
    string? FailureMessage,
    PaymentInvoiceResponse? Invoice,
    IReadOnlyList<PaymentRefundResponse> Refunds);

public sealed record PaymentCheckoutResponse(
    PaymentTransactionResponse Transaction,
    IReadOnlyList<PaymentGatewayOptionResponse> AvailableGateways,
    string HostedPaymentUrl);

public sealed record PaymentInvoicePdfResponse(
    byte[] Content,
    string FileName,
    string ContentType);
