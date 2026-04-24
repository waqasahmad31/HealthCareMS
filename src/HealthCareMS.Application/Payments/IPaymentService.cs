using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Payments;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentGatewayOptionResponse>> GetGatewayOptionsAsync(CancellationToken cancellationToken);

    Task<Result<PaymentCheckoutResponse>> CreatePharmacyOrderCheckoutAsync(
        Guid orderId,
        CreateOrderPaymentRequest request,
        CancellationToken cancellationToken);

    Task<Result<PaymentTransactionResponse>> GetOrderPaymentAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Result<PaymentTransactionResponse>> ProcessGatewayWebhookAsync(
        string gateway,
        string? signature,
        PaymentWebhookRequest request,
        CancellationToken cancellationToken);

    Task<Result<PaymentTransactionResponse>> SimulateGatewayCallbackAsync(
        Guid transactionId,
        SimulatePaymentCallbackRequest request,
        CancellationToken cancellationToken);

    Task<Result<PaymentRefundResponse>> RefundTransactionAsync(
        Guid transactionId,
        RefundPaymentRequest request,
        CancellationToken cancellationToken);

    Task<Result<PaymentInvoiceResponse>> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<Result<PaymentInvoicePdfResponse>> GenerateInvoicePdfAsync(Guid invoiceId, CancellationToken cancellationToken);
}
