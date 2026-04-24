using HealthCareMS.API.Security;
using HealthCareMS.Application.Payments;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ApiControllerBase
{
    [HttpGet("gateways")]
    [Authorize]
    public async Task<IActionResult> GetGateways(CancellationToken cancellationToken)
    {
        var result = await paymentService.GetGatewayOptionsAsync(cancellationToken);
        return OkEnvelope(result);
    }

    [HttpPost("pharmacy-orders/{orderId:guid}/checkout")]
    [Authorize]
    public async Task<IActionResult> CreatePharmacyOrderCheckout(
        Guid orderId,
        CreateOrderPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.CreatePharmacyOrderCheckoutAsync(orderId, request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("pharmacy-orders/{orderId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetOrderPayment(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetOrderPaymentAsync(orderId, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("webhooks/{gateway}")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessGatewayWebhook(
        string gateway,
        [FromHeader(Name = "X-Webhook-Signature")] string? signature,
        PaymentWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.ProcessGatewayWebhookAsync(gateway, signature, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("transactions/{transactionId:guid}/simulate")]
    [RequirePermission(PermissionKeys.Payment.InvoicesView)]
    public async Task<IActionResult> SimulateCallback(
        Guid transactionId,
        SimulatePaymentCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.SimulateGatewayCallbackAsync(transactionId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("transactions/{transactionId:guid}/refund")]
    [RequirePermission(PermissionKeys.Payment.RefundInitiate)]
    public async Task<IActionResult> Refund(
        Guid transactionId,
        RefundPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.RefundTransactionAsync(transactionId, request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("invoices/{invoiceId:guid}")]
    [RequirePermission(PermissionKeys.Payment.InvoicesView)]
    public async Task<IActionResult> GetInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetInvoiceAsync(invoiceId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("invoices/{invoiceId:guid}/invoice.pdf")]
    [RequirePermission(PermissionKeys.Payment.InvoicesView)]
    public async Task<IActionResult> DownloadInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await paymentService.GenerateInvoicePdfAsync(invoiceId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }
}
