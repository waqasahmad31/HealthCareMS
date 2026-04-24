using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Pharmacy;

namespace HealthCareMS.Domain.Payments;

public enum PaymentGateway
{
    JazzCash = 1,
    EasyPaisa = 2,
    Stripe = 3
}

public enum PaymentTransactionStatus
{
    Pending = 1,
    AwaitingCustomerAction = 2,
    Succeeded = 3,
    Failed = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}

public enum InvoiceStatus
{
    Issued = 1,
    Paid = 2,
    PartiallyRefunded = 3,
    Refunded = 4
}

public enum RefundStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

public sealed class PaymentTransaction : BaseEntity
{
    public Guid? TenantId { get; set; }

    public Guid? PharmacyOrderId { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    public Guid ReferenceId { get; set; }

    public string PaymentNumber { get; set; } = string.Empty;

    public PaymentGateway Gateway { get; set; }

    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "PKR";

    public string SessionToken { get; set; } = string.Empty;

    public string CheckoutUrl { get; set; } = string.Empty;

    public string? ExternalReference { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public string? LastWebhookPayload { get; set; }

    public DateTimeOffset? LastWebhookReceivedAt { get; set; }

    public Tenant? Tenant { get; set; }

    public PharmacyOrder? PharmacyOrder { get; set; }

    public PaymentInvoice? Invoice { get; set; }

    public ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
}

public sealed class PaymentInvoice : BaseEntity
{
    public Guid? TenantId { get; set; }

    public Guid PaymentTransactionId { get; set; }

    public Guid? PharmacyOrderId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;

    public string BillingName { get; set; } = string.Empty;

    public string? BillingEmail { get; set; }

    public string? BillingPhone { get; set; }

    public string? BillingAddress { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "PKR";

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? RefundedAt { get; set; }

    public string? Notes { get; set; }

    public Tenant? Tenant { get; set; }

    public PaymentTransaction PaymentTransaction { get; set; } = null!;

    public PharmacyOrder? PharmacyOrder { get; set; }

    public ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
}

public sealed class PaymentRefund : BaseEntity
{
    public Guid PaymentTransactionId { get; set; }

    public Guid? PaymentInvoiceId { get; set; }

    public string RefundNumber { get; set; } = string.Empty;

    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "PKR";

    public string Reason { get; set; } = string.Empty;

    public string? ExternalReference { get; set; }

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public PaymentTransaction PaymentTransaction { get; set; } = null!;

    public PaymentInvoice? PaymentInvoice { get; set; }

    public ApplicationUser? RequestedByUser { get; set; }
}
