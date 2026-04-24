using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Payments;
using HealthCareMS.Domain.Payments;
using HealthCareMS.Domain.Pharmacy;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HealthCareMS.Infrastructure.Payments;

public sealed class PaymentService(
    HealthCareDbContext dbContext,
    IOptions<PaymentGatewayOptions> options,
    ICurrentUser? currentUser = null) : IPaymentService
{
    private readonly PaymentGatewayOptions gatewayOptions = options.Value;

    public Task<IReadOnlyList<PaymentGatewayOptionResponse>> GetGatewayOptionsAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Task.FromResult((IReadOnlyList<PaymentGatewayOptionResponse>)
        [
            Map(PaymentGateway.JazzCash, gatewayOptions.JazzCash),
            Map(PaymentGateway.EasyPaisa, gatewayOptions.EasyPaisa),
            Map(PaymentGateway.Stripe, gatewayOptions.Stripe)
        ]);
    }

    public async Task<Result<PaymentCheckoutResponse>> CreatePharmacyOrderCheckoutAsync(
        Guid orderId,
        CreateOrderPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PaymentGateway>(request.Gateway, ignoreCase: true, out var gateway))
        {
            return Result<PaymentCheckoutResponse>.Failure(new Error("PAYMENT_GATEWAY_INVALID", "Gateway is invalid."));
        }

        var provider = ResolveGateway(gateway);
        if (!provider.Enabled)
        {
            return Result<PaymentCheckoutResponse>.Failure(new Error("PAYMENT_GATEWAY_DISABLED", $"{provider.DisplayName} is not enabled."));
        }

        var order = await OrderQuery()
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
        {
            return Result<PaymentCheckoutResponse>.Failure(new Error("PAYMENT_ORDER_NOT_FOUND", "Pharmacy order was not found."));
        }

        if (order.Status is PharmacyOrderStatus.Cancelled or PharmacyOrderStatus.Rejected)
        {
            return Result<PaymentCheckoutResponse>.Failure(new Error("PAYMENT_ORDER_INVALID", "Payments cannot be started for cancelled or rejected orders."));
        }

        var activeTransaction = await PaymentQuery()
            .Where(x => x.PharmacyOrderId == orderId
                && x.Gateway == gateway
                && x.Status == PaymentTransactionStatus.AwaitingCustomerAction
                && x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeTransaction is not null)
        {
            return Result<PaymentCheckoutResponse>.Success(new PaymentCheckoutResponse(
                Map(activeTransaction),
                await GetGatewayOptionsAsync(cancellationToken),
                activeTransaction.CheckoutUrl));
        }

        var now = DateTimeOffset.UtcNow;
        var sessionToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var transaction = new PaymentTransaction
        {
            TenantId = order.TenantId,
            PharmacyOrderId = order.Id,
            PharmacyOrder = order,
            ReferenceType = nameof(PharmacyOrder),
            ReferenceId = order.Id,
            PaymentNumber = await GeneratePaymentNumberAsync(now, cancellationToken),
            Gateway = gateway,
            Status = PaymentTransactionStatus.AwaitingCustomerAction,
            Amount = order.TotalAmount,
            Currency = gatewayOptions.Currency,
            SessionToken = sessionToken,
            CheckoutUrl = BuildCheckoutUrl(provider.CheckoutBaseUrl, sessionToken, order.OrderNumber, order.TotalAmount, request.ReturnUrl, request.CancelUrl),
            ExpiresAt = now.AddMinutes(30),
            MetadataJson = BuildMetadataJson(request.CustomerIpAddress)
        };

        var invoice = new PaymentInvoice
        {
            TenantId = order.TenantId,
            PaymentTransaction = transaction,
            PharmacyOrderId = order.Id,
            PharmacyOrder = order,
            InvoiceNumber = await GenerateInvoiceNumberAsync(now, cancellationToken),
            BillingName = order.Patient.User.FullName,
            BillingEmail = order.Patient.User.Email,
            BillingPhone = order.Patient.Phone ?? order.Patient.User.PhoneNumber,
            BillingAddress = order.DeliveryAddress,
            SubTotal = order.SubTotal,
            DeliveryFee = order.DeliveryFee,
            TaxAmount = 0m,
            DiscountAmount = 0m,
            TotalAmount = order.TotalAmount,
            Currency = gatewayOptions.Currency,
            IssuedAt = now,
            Status = InvoiceStatus.Issued,
            Notes = "Pharmacy order invoice generated from checkout session."
        };

        dbContext.PaymentTransactions.Add(transaction);
        dbContext.PaymentInvoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await PaymentQuery().SingleAsync(x => x.Id == transaction.Id, cancellationToken);
        return Result<PaymentCheckoutResponse>.Success(new PaymentCheckoutResponse(
            Map(saved),
            await GetGatewayOptionsAsync(cancellationToken),
            saved.CheckoutUrl));
    }

    public async Task<Result<PaymentTransactionResponse>> GetOrderPaymentAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var orderExists = await dbContext.PharmacyOrders.AnyAsync(x => x.Id == orderId, cancellationToken);
        if (!orderExists)
        {
            return Result<PaymentTransactionResponse>.Failure(new Error("PAYMENT_ORDER_NOT_FOUND", "Pharmacy order was not found."));
        }

        var transaction = await PaymentQuery()
            .Where(x => x.PharmacyOrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return transaction is null
            ? Result<PaymentTransactionResponse>.Failure(new Error("PAYMENT_TRANSACTION_NOT_FOUND", "No payment transaction was found for the order."))
            : Result<PaymentTransactionResponse>.Success(Map(transaction));
    }

    public async Task<Result<PaymentTransactionResponse>> ProcessGatewayWebhookAsync(
        string gateway,
        string? signature,
        PaymentWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PaymentGateway>(gateway, ignoreCase: true, out var parsedGateway))
        {
            return Result<PaymentTransactionResponse>.Failure(new Error("PAYMENT_GATEWAY_INVALID", "Gateway is invalid."));
        }

        var provider = ResolveGateway(parsedGateway);
        var expectedSignature = ComputeSignature(provider.WebhookSecret, request);
        if (!string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase))
        {
            return Result<PaymentTransactionResponse>.Failure(new Error("PAYMENT_WEBHOOK_SIGNATURE_INVALID", "Webhook signature validation failed."));
        }

        var transaction = await PaymentQuery()
            .SingleOrDefaultAsync(x => x.SessionToken == request.SessionToken && x.Gateway == parsedGateway, cancellationToken);

        if (transaction is null)
        {
            return Result<PaymentTransactionResponse>.Failure(new Error("PAYMENT_TRANSACTION_NOT_FOUND", "Payment transaction was not found."));
        }

        if (Math.Abs(transaction.Amount - request.Amount) > 0.01m)
        {
            return Result<PaymentTransactionResponse>.Failure(new Error("PAYMENT_AMOUNT_MISMATCH", "Webhook amount did not match the transaction amount."));
        }

        transaction.LastWebhookPayload = string.IsNullOrWhiteSpace(request.PayloadJson)
            ? BuildWebhookPayloadJson(request)
            : request.PayloadJson;
        transaction.LastWebhookReceivedAt = DateTimeOffset.UtcNow;
        transaction.ExternalReference = string.IsNullOrWhiteSpace(request.ExternalReference)
            ? transaction.ExternalReference
            : request.ExternalReference.Trim();

        var normalizedStatus = request.Status.Trim();
        if (string.Equals(normalizedStatus, "Succeeded", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedStatus, "Paid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            transaction.Status = CompletedRefundAmount(transaction) switch
            {
                > 0m when CompletedRefundAmount(transaction) < transaction.Amount => PaymentTransactionStatus.PartiallyRefunded,
                > 0m => PaymentTransactionStatus.Refunded,
                _ => PaymentTransactionStatus.Succeeded
            };
            transaction.PaidAt ??= DateTimeOffset.UtcNow;
            transaction.FailedAt = null;
            transaction.FailureCode = null;
            transaction.FailureMessage = null;
            if (transaction.Invoice is not null)
            {
                transaction.Invoice.Status = transaction.Status switch
                {
                    PaymentTransactionStatus.Refunded => InvoiceStatus.Refunded,
                    PaymentTransactionStatus.PartiallyRefunded => InvoiceStatus.PartiallyRefunded,
                    _ => InvoiceStatus.Paid
                };
                transaction.Invoice.PaidAt ??= transaction.PaidAt;
            }
        }
        else
        {
            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.FailedAt = DateTimeOffset.UtcNow;
            transaction.FailureCode = Normalize(request.FailureCode) ?? "PAYMENT_FAILED";
            transaction.FailureMessage = Normalize(request.FailureMessage) ?? $"Gateway reported status {normalizedStatus}.";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentTransactionResponse>.Success(Map(transaction));
    }

    public async Task<Result<PaymentTransactionResponse>> SimulateGatewayCallbackAsync(
        Guid transactionId,
        SimulatePaymentCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await PaymentQuery().SingleOrDefaultAsync(x => x.Id == transactionId, cancellationToken);
        if (transaction is null)
        {
            return Result<PaymentTransactionResponse>.Failure(new Error("PAYMENT_TRANSACTION_NOT_FOUND", "Payment transaction was not found."));
        }

        var provider = ResolveGateway(transaction.Gateway);
        var webhookRequest = new PaymentWebhookRequest(
            transaction.SessionToken,
            "CheckoutCompleted",
            request.Status,
            transaction.Amount,
            transaction.Currency,
            Normalize(request.ExternalReference) ?? $"{transaction.Gateway}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            null,
            null,
            null);

        return await ProcessGatewayWebhookAsync(
            transaction.Gateway.ToString(),
            ComputeSignature(provider.WebhookSecret, webhookRequest),
            webhookRequest,
            cancellationToken);
    }

    public async Task<Result<PaymentRefundResponse>> RefundTransactionAsync(
        Guid transactionId,
        RefundPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<PaymentRefundResponse>.Failure(new Error("PAYMENT_REFUND_REASON_REQUIRED", "Reason is required."));
        }

        var transaction = await PaymentQuery().SingleOrDefaultAsync(x => x.Id == transactionId, cancellationToken);
        if (transaction is null)
        {
            return Result<PaymentRefundResponse>.Failure(new Error("PAYMENT_TRANSACTION_NOT_FOUND", "Payment transaction was not found."));
        }

        if (transaction.Status is not (PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.PartiallyRefunded))
        {
            return Result<PaymentRefundResponse>.Failure(new Error("PAYMENT_REFUND_INVALID", "Only paid transactions can be refunded."));
        }

        var refundedAmount = CompletedRefundAmount(transaction);
        var remaining = transaction.Amount - refundedAmount;
        var requestedAmount = request.Amount ?? remaining;
        if (requestedAmount <= 0m || requestedAmount > remaining)
        {
            return Result<PaymentRefundResponse>.Failure(new Error("PAYMENT_REFUND_AMOUNT_INVALID", "Refund amount exceeds the remaining refundable balance."));
        }

        var refund = new PaymentRefund
        {
            PaymentTransactionId = transaction.Id,
            PaymentTransaction = transaction,
            PaymentInvoiceId = transaction.Invoice?.Id,
            PaymentInvoice = transaction.Invoice,
            RefundNumber = await GenerateRefundNumberAsync(DateTimeOffset.UtcNow, cancellationToken),
            Amount = requestedAmount,
            Currency = transaction.Currency,
            Reason = request.Reason.Trim(),
            Status = RefundStatus.Completed,
            ExternalReference = $"RF-{transaction.Gateway}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            RequestedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            RequestedByUserId = currentUser?.UserId
        };

        dbContext.PaymentRefunds.Add(refund);

        var totalRefunded = refundedAmount + requestedAmount;
        transaction.Status = totalRefunded >= transaction.Amount
            ? PaymentTransactionStatus.Refunded
            : PaymentTransactionStatus.PartiallyRefunded;

        if (transaction.Invoice is not null)
        {
            transaction.Invoice.Status = totalRefunded >= transaction.Amount
                ? InvoiceStatus.Refunded
                : InvoiceStatus.PartiallyRefunded;
            transaction.Invoice.RefundedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentRefundResponse>.Success(Map(refund));
    }

    public async Task<Result<PaymentInvoiceResponse>> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await InvoiceQuery().SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        return invoice is null
            ? Result<PaymentInvoiceResponse>.Failure(new Error("PAYMENT_INVOICE_NOT_FOUND", "Invoice was not found."))
            : Result<PaymentInvoiceResponse>.Success(Map(invoice));
    }

    public async Task<Result<PaymentInvoicePdfResponse>> GenerateInvoicePdfAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await InvoiceQuery().SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result<PaymentInvoicePdfResponse>.Failure(new Error("PAYMENT_INVOICE_NOT_FOUND", "Invoice was not found."));
        }

        var pdf = GenerateInvoicePdf(invoice);
        return Result<PaymentInvoicePdfResponse>.Success(new PaymentInvoicePdfResponse(
            pdf,
            $"{invoice.InvoiceNumber}.pdf",
            "application/pdf"));
    }

    private IQueryable<PharmacyOrder> OrderQuery()
    {
        return dbContext.PharmacyOrders
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Items);
    }

    private IQueryable<PaymentTransaction> PaymentQuery()
    {
        return dbContext.PaymentTransactions
            .Include(x => x.PharmacyOrder)
            .ThenInclude(x => x!.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.PharmacyOrder)
            .ThenInclude(x => x!.Items)
            .Include(x => x.Invoice)
            .Include(x => x.Refunds.OrderByDescending(r => r.RequestedAt));
    }

    private IQueryable<PaymentInvoice> InvoiceQuery()
    {
        return dbContext.PaymentInvoices
            .Include(x => x.PaymentTransaction)
            .ThenInclude(x => x.Refunds.OrderByDescending(r => r.RequestedAt))
            .Include(x => x.PharmacyOrder)
            .ThenInclude(x => x!.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.PharmacyOrder)
            .ThenInclude(x => x!.Items);
    }

    private PaymentGatewayOptions.GatewayOptions ResolveGateway(PaymentGateway gateway)
    {
        return gateway switch
        {
            PaymentGateway.JazzCash => gatewayOptions.JazzCash,
            PaymentGateway.EasyPaisa => gatewayOptions.EasyPaisa,
            PaymentGateway.Stripe => gatewayOptions.Stripe,
            _ => throw new InvalidOperationException($"Unsupported gateway {gateway}.")
        };
    }

    private static PaymentGatewayOptionResponse Map(PaymentGateway gateway, PaymentGatewayOptions.GatewayOptions options)
    {
        return new PaymentGatewayOptionResponse(
            gateway.ToString(),
            options.DisplayName,
            options.Enabled,
            options.CheckoutBaseUrl);
    }

    private static PaymentTransactionResponse Map(PaymentTransaction transaction)
    {
        return new PaymentTransactionResponse(
            transaction.Id,
            transaction.TenantId,
            transaction.PharmacyOrderId,
            transaction.ReferenceType,
            transaction.ReferenceId,
            transaction.PaymentNumber,
            transaction.Gateway.ToString(),
            transaction.Status.ToString(),
            transaction.Amount,
            transaction.Currency,
            transaction.SessionToken,
            transaction.CheckoutUrl,
            transaction.ExternalReference,
            transaction.ExpiresAt,
            transaction.PaidAt,
            transaction.FailedAt,
            transaction.FailureCode,
            transaction.FailureMessage,
            transaction.Invoice is null ? null : Map(transaction.Invoice),
            transaction.Refunds.OrderByDescending(x => x.RequestedAt).Select(Map).ToList());
    }

    private static PaymentInvoiceResponse Map(PaymentInvoice invoice)
    {
        return new PaymentInvoiceResponse(
            invoice.Id,
            invoice.TenantId,
            invoice.PaymentTransactionId,
            invoice.PharmacyOrderId,
            invoice.InvoiceNumber,
            invoice.Status.ToString(),
            invoice.BillingName,
            invoice.BillingEmail,
            invoice.BillingPhone,
            invoice.BillingAddress,
            invoice.SubTotal,
            invoice.DeliveryFee,
            invoice.TaxAmount,
            invoice.DiscountAmount,
            invoice.TotalAmount,
            invoice.Currency,
            invoice.IssuedAt,
            invoice.PaidAt,
            invoice.RefundedAt,
            invoice.Notes);
    }

    private static PaymentRefundResponse Map(PaymentRefund refund)
    {
        return new PaymentRefundResponse(
            refund.Id,
            refund.RefundNumber,
            refund.Status.ToString(),
            refund.Amount,
            refund.Currency,
            refund.Reason,
            refund.ExternalReference,
            refund.RequestedAt,
            refund.CompletedAt);
    }

    private static string BuildCheckoutUrl(
        string baseUrl,
        string sessionToken,
        string orderNumber,
        decimal amount,
        string? returnUrl,
        string? cancelUrl)
    {
        var builder = new StringBuilder(baseUrl);
        builder.Append(baseUrl.Contains('?') ? '&' : '?');
        builder.Append($"session={Uri.EscapeDataString(sessionToken)}");
        builder.Append($"&order={Uri.EscapeDataString(orderNumber)}");
        builder.Append($"&amount={Uri.EscapeDataString(amount.ToString("0.00", CultureInfo.InvariantCulture))}");
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            builder.Append($"&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (!string.IsNullOrWhiteSpace(cancelUrl))
        {
            builder.Append($"&cancelUrl={Uri.EscapeDataString(cancelUrl)}");
        }

        return builder.ToString();
    }

    private static string BuildMetadataJson(string? customerIpAddress)
    {
        return $$"""
                 {
                   "customerIpAddress": "{{Normalize(customerIpAddress) ?? string.Empty}}"
                 }
                 """;
    }

    private static string BuildWebhookPayloadJson(PaymentWebhookRequest request)
    {
        return $$"""
                 {
                   "sessionToken": "{{request.SessionToken}}",
                   "eventType": "{{request.EventType}}",
                   "status": "{{request.Status}}",
                   "amount": {{request.Amount.ToString("0.00", CultureInfo.InvariantCulture)}},
                   "currency": "{{request.Currency ?? string.Empty}}",
                   "externalReference": "{{request.ExternalReference ?? string.Empty}}"
                 }
                 """;
    }

    private static string ComputeSignature(string secret, PaymentWebhookRequest request)
    {
        var payload = string.Join('|',
            request.SessionToken.Trim(),
            request.EventType.Trim(),
            request.Status.Trim(),
            request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            Normalize(request.Currency) ?? string.Empty,
            Normalize(request.ExternalReference) ?? string.Empty);

        var bytes = Encoding.UTF8.GetBytes(payload);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(secretBytes);
        return Convert.ToHexString(hmac.ComputeHash(bytes));
    }

    private static decimal CompletedRefundAmount(PaymentTransaction transaction)
    {
        return transaction.Refunds
            .Where(x => x.Status == RefundStatus.Completed)
            .Sum(x => x.Amount);
    }

    private async Task<string> GeneratePaymentNumberAsync(DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        var prefix = $"PAY-{timestamp:yyyyMMdd}-";
        var count = await dbContext.PaymentTransactions.CountAsync(x => x.PaymentNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private async Task<string> GenerateInvoiceNumberAsync(DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        var prefix = $"INV-{timestamp:yyyyMMdd}-";
        var count = await dbContext.PaymentInvoices.CountAsync(x => x.InvoiceNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private async Task<string> GenerateRefundNumberAsync(DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        var prefix = $"RF-{timestamp:yyyyMMdd}-";
        var count = await dbContext.PaymentRefunds.CountAsync(x => x.RefundNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private static byte[] GenerateInvoicePdf(PaymentInvoice invoice)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text("HealthCareMS Pharmacy Invoice").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Invoice: {invoice.InvoiceNumber}");
                    column.Item().Text($"Payment: {invoice.PaymentTransaction.PaymentNumber} | {invoice.PaymentTransaction.Gateway}");
                    column.Item().Text($"Status: {invoice.Status}");
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(2);
                            left.Item().Text("Billed To").Bold();
                            left.Item().Text(invoice.BillingName);
                            if (!string.IsNullOrWhiteSpace(invoice.BillingEmail))
                            {
                                left.Item().Text(invoice.BillingEmail);
                            }

                            if (!string.IsNullOrWhiteSpace(invoice.BillingPhone))
                            {
                                left.Item().Text(invoice.BillingPhone);
                            }

                            if (!string.IsNullOrWhiteSpace(invoice.BillingAddress))
                            {
                                left.Item().Text(invoice.BillingAddress);
                            }
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Spacing(2);
                            right.Item().Text("Issue Details").Bold();
                            right.Item().Text($"Issued: {invoice.IssuedAt:yyyy-MM-dd HH:mm} UTC");
                            if (invoice.PaidAt.HasValue)
                            {
                                right.Item().Text($"Paid: {invoice.PaidAt:yyyy-MM-dd HH:mm} UTC");
                            }

                            if (invoice.RefundedAt.HasValue)
                            {
                                right.Item().Text($"Refunded: {invoice.RefundedAt:yyyy-MM-dd HH:mm} UTC");
                            }

                            right.Item().Text($"Currency: {invoice.Currency}");
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Item");
                            header.Cell().Element(HeaderCell).Text("Qty");
                            header.Cell().Element(HeaderCell).Text("Unit");
                            header.Cell().Element(HeaderCell).Text("Line Total");
                        });

                        var invoiceItems = invoice.PharmacyOrder?.Items.OrderBy(x => x.MedicineName)
                            ?? Enumerable.Empty<PharmacyOrderItem>();
                        foreach (var item in invoiceItems)
                        {
                            table.Cell().Element(BodyCell).Text(item.MedicineName);
                            table.Cell().Element(BodyCell).Text(item.Quantity.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Element(BodyCell).Text(item.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture));
                            table.Cell().Element(BodyCell).Text(item.LineTotal.ToString("0.00", CultureInfo.InvariantCulture));
                        }

                        table.Cell().Element(BodyCell).Text("Delivery Fee");
                        table.Cell().Element(BodyCell).Text("-");
                        table.Cell().Element(BodyCell).Text("-");
                        table.Cell().Element(BodyCell).Text(invoice.DeliveryFee.ToString("0.00", CultureInfo.InvariantCulture));
                    });

                    column.Item().AlignRight().Column(summary =>
                    {
                        summary.Spacing(3);
                        summary.Item().Text($"SubTotal: {invoice.SubTotal:0.00}");
                        summary.Item().Text($"Delivery: {invoice.DeliveryFee:0.00}");
                        summary.Item().Text($"Tax: {invoice.TaxAmount:0.00}");
                        summary.Item().Text($"Discount: {invoice.DiscountAmount:0.00}");
                        summary.Item().Text($"Total: {invoice.TotalAmount:0.00}").FontSize(13).Bold();
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        column.Item().Text("Notes").Bold();
                        column.Item().Text(invoice.Notes);
                    }
                });

                page.Footer().AlignCenter().Text($"Generated by HealthCareMS at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC");
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Blue.Lighten4)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(5);
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
