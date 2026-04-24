namespace HealthCareMS.Blazor.Models;

public sealed record PaymentGatewayOptionModel(
    string Gateway,
    string DisplayName,
    bool IsEnabled,
    string CheckoutBaseUrl);

public sealed class CreateOrderPaymentFormModel
{
    public string Gateway { get; set; } = "JazzCash";

    public string? ReturnUrl { get; set; }

    public string? CancelUrl { get; set; }

    public string? CustomerIpAddress { get; set; }
}

public sealed record PaymentRefundModel(
    Guid Id,
    string RefundNumber,
    string Status,
    decimal Amount,
    string Currency,
    string Reason,
    string? ExternalReference,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt);

public sealed record PaymentInvoiceModel(
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

public sealed record PaymentTransactionModel(
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
    PaymentInvoiceModel? Invoice,
    IReadOnlyList<PaymentRefundModel> Refunds);

public sealed record PaymentCheckoutModel(
    PaymentTransactionModel Transaction,
    IReadOnlyList<PaymentGatewayOptionModel> AvailableGateways,
    string HostedPaymentUrl);

public sealed class SimulatePaymentCallbackFormModel
{
    public string Status { get; set; } = "Succeeded";

    public string? ExternalReference { get; set; }
}

public sealed class RefundPaymentFormModel
{
    public decimal? Amount { get; set; }

    public string Reason { get; set; } = "Customer requested refund.";
}
