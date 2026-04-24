using System.Globalization;
using System.Text;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Domain.Payments;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HealthCareMS.Infrastructure.Pharmacy;

public sealed partial class PharmacyService
{
    public async Task<Result<PharmacyReportsDashboardResponse>> GetReportsAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var window = ResolveWindow(from, to);
        var fromStart = new DateTimeOffset(window.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toEndExclusive = new DateTimeOffset(window.To.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var orders = await dbContext.PharmacyOrders
            .Include(x => x.Items)
            .Where(x => x.OrderedAt >= fromStart && x.OrderedAt < toEndExclusive)
            .ToListAsync(cancellationToken);

        var dispenses = await dbContext.PrescriptionDispenses
            .Include(x => x.Items)
            .Where(x => x.DispensedAt >= fromStart && x.DispensedAt < toEndExclusive)
            .ToListAsync(cancellationToken);

        var payments = await dbContext.PaymentTransactions
            .Include(x => x.Invoice)
            .Include(x => x.Refunds)
            .Include(x => x.PharmacyOrder)
            .Where(x =>
                (x.PaidAt.HasValue && x.PaidAt.Value >= fromStart && x.PaidAt.Value < toEndExclusive)
                || x.CreatedAt >= fromStart && x.CreatedAt < toEndExclusive)
            .ToListAsync(cancellationToken);

        var stockBatches = await dbContext.StockBatches
            .Include(x => x.Medicine)
            .Where(x => x.QuantityOnHand > 0)
            .ToListAsync(cancellationToken);

        var salesByPeriod = BuildSalesPoints(window.From, window.To, orders, dispenses, payments);
        var topMedicines = BuildTopMedicines(orders, dispenses);
        var stockValuationItems = BuildStockValuationItems(stockBatches);
        var expiryItems = BuildExpiryReportItems(stockBatches);
        var reconciliationItems = BuildReconciliationItems(payments);

        var totalRevenue = payments
            .Where(x => x.Status is PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.PartiallyRefunded or PaymentTransactionStatus.Refunded)
            .Sum(x => x.Amount);

        var totalRefunded = payments.Sum(x => x.Refunds.Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount));
        var stockValuation = stockValuationItems.Sum(x => x.StockValue);

        return Result<PharmacyReportsDashboardResponse>.Success(new PharmacyReportsDashboardResponse(
            window.From,
            window.To,
            totalRevenue,
            totalRefunded,
            orders.Count,
            dispenses.Count,
            stockValuation,
            salesByPeriod,
            topMedicines,
            stockValuationItems,
            expiryItems,
            reconciliationItems));
    }

    public async Task<Result<ReportExportResponse>> ExportReportAsync(
        string reportType,
        DateOnly? from,
        DateOnly? to,
        string format,
        CancellationToken cancellationToken)
    {
        var dashboard = await GetReportsAsync(from, to, cancellationToken);
        if (dashboard.IsFailure)
        {
            return Result<ReportExportResponse>.Failure(dashboard.Error);
        }

        var normalizedReport = reportType.Trim().ToLowerInvariant();
        var normalizedFormat = format.Trim().ToLowerInvariant();

        return normalizedFormat switch
        {
            "csv" => Result<ReportExportResponse>.Success(new ReportExportResponse(
                BuildCsv(normalizedReport, dashboard.Value),
                $"pharmacy-{normalizedReport}-{dashboard.Value.From:yyyyMMdd}-{dashboard.Value.To:yyyyMMdd}.csv",
                "text/csv")),
            "pdf" => Result<ReportExportResponse>.Success(new ReportExportResponse(
                BuildPdf(normalizedReport, dashboard.Value),
                $"pharmacy-{normalizedReport}-{dashboard.Value.From:yyyyMMdd}-{dashboard.Value.To:yyyyMMdd}.pdf",
                "application/pdf")),
            _ => Result<ReportExportResponse>.Failure(new Error("PHARMACY_REPORT_FORMAT_INVALID", "Format must be csv or pdf."))
        };
    }

    private static (DateOnly From, DateOnly To) ResolveWindow(DateOnly? from, DateOnly? to)
    {
        var resolvedTo = to ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var resolvedFrom = from ?? resolvedTo.AddDays(-29);
        if (resolvedFrom > resolvedTo)
        {
            (resolvedFrom, resolvedTo) = (resolvedTo, resolvedFrom);
        }

        return (resolvedFrom, resolvedTo);
    }

    private static IReadOnlyList<PharmacySalesPointResponse> BuildSalesPoints(
        DateOnly from,
        DateOnly to,
        IReadOnlyList<Domain.Pharmacy.PharmacyOrder> orders,
        IReadOnlyList<Domain.Pharmacy.PrescriptionDispense> dispenses,
        IReadOnlyList<PaymentTransaction> payments)
    {
        var result = new List<PharmacySalesPointResponse>();
        for (var cursor = from; cursor <= to; cursor = cursor.AddDays(1))
        {
            var dayRevenue = payments
                .Where(x => x.PaidAt.HasValue && DateOnly.FromDateTime(x.PaidAt.Value.UtcDateTime.Date) == cursor)
                .Sum(x => x.Amount - x.Refunds.Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount));

            var dayOrders = orders.Count(x => DateOnly.FromDateTime(x.OrderedAt.UtcDateTime.Date) == cursor);
            var dayDispenses = dispenses.Count(x => DateOnly.FromDateTime(x.DispensedAt.UtcDateTime.Date) == cursor);

            result.Add(new PharmacySalesPointResponse(cursor, dayRevenue, dayOrders, dayDispenses));
        }

        return result;
    }

    private static IReadOnlyList<TopMedicineReportResponse> BuildTopMedicines(
        IReadOnlyList<Domain.Pharmacy.PharmacyOrder> orders,
        IReadOnlyList<Domain.Pharmacy.PrescriptionDispense> dispenses)
    {
        var fromOrders = orders
            .SelectMany(x => x.Items.Select(item => new { item.MedicineId, item.MedicineName, Quantity = item.Quantity, Revenue = item.LineTotal }));
        var fromDispenses = dispenses
            .SelectMany(x => x.Items.Select(item => new
            {
                item.MedicineId,
                MedicineName = item.DispensedMedicineName,
                Quantity = item.QuantityDispensed,
                Revenue = item.LineTotal
            }));

        return fromOrders
            .Concat(fromDispenses)
            .GroupBy(x => new { x.MedicineId, x.MedicineName })
            .Select(x => new TopMedicineReportResponse(
                x.Key.MedicineId,
                x.Key.MedicineName,
                x.Sum(item => item.Quantity),
                x.Sum(item => item.Revenue)))
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.QuantitySold)
            .Take(10)
            .ToList();
    }

    private static IReadOnlyList<StockValuationReportResponse> BuildStockValuationItems(IReadOnlyList<Domain.Pharmacy.StockBatch> stockBatches)
    {
        return stockBatches
            .GroupBy(x => new { x.MedicineId, x.Medicine.BrandName })
            .Select(group =>
            {
                var stock = group.Sum(x => x.QuantityOnHand);
                var costBasis = stock == 0
                    ? 0m
                    : group.Sum(x => x.QuantityOnHand * x.UnitCostPrice) / stock;

                return new StockValuationReportResponse(
                    group.Key.MedicineId,
                    group.Key.BrandName,
                    stock,
                    decimal.Round(costBasis, 2),
                    decimal.Round(group.Sum(x => x.QuantityOnHand * x.UnitCostPrice), 2),
                    group.OrderBy(x => x.ExpiryDate).Select(x => (DateOnly?)x.ExpiryDate).FirstOrDefault());
            })
            .OrderByDescending(x => x.StockValue)
            .ToList();
    }

    private static IReadOnlyList<ExpiryReportResponse> BuildExpiryReportItems(IReadOnlyList<Domain.Pharmacy.StockBatch> stockBatches)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return stockBatches
            .Where(x => x.QuantityOnHand > 0)
            .Select(x =>
            {
                var days = (x.ExpiryDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;
                var severity = days switch
                {
                    <= 30 => "Critical",
                    <= 60 => "Warning",
                    _ => "Info"
                };

                return new ExpiryReportResponse(
                    x.Id,
                    x.MedicineId,
                    x.Medicine.BrandName,
                    x.BatchNumber,
                    x.ExpiryDate,
                    x.QuantityOnHand,
                    days,
                    decimal.Round(x.QuantityOnHand * x.UnitCostPrice, 2),
                    severity);
            })
            .OrderBy(x => x.DaysToExpiry)
            .Take(100)
            .ToList();
    }

    private static IReadOnlyList<ReconciliationReportResponse> BuildReconciliationItems(IReadOnlyList<PaymentTransaction> payments)
    {
        return payments
            .OrderByDescending(x => x.PaidAt ?? x.CreatedAt)
            .Take(200)
            .Select(x =>
            {
                var refunded = x.Refunds.Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount);
                var reconciliationStatus = x.Invoice is null
                    ? "MissingInvoice"
                    : Math.Abs((x.Invoice.TotalAmount - x.Amount)) <= 0.01m
                        ? "Matched"
                        : "AmountMismatch";

                return new ReconciliationReportResponse(
                    x.Id,
                    x.PaymentNumber,
                    x.Gateway.ToString(),
                    x.Status.ToString(),
                    x.Amount,
                    refunded,
                    x.Amount - refunded,
                    x.PaidAt,
                    x.PharmacyOrder?.OrderNumber,
                    x.Invoice?.InvoiceNumber,
                    reconciliationStatus);
            })
            .ToList();
    }

    private static byte[] BuildCsv(string reportType, PharmacyReportsDashboardResponse dashboard)
    {
        var rows = reportType switch
        {
            "sales" => dashboard.SalesByPeriod.Select(x => string.Join(',',
                x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x.Revenue.ToString("0.00", CultureInfo.InvariantCulture),
                x.Orders.ToString(CultureInfo.InvariantCulture),
                x.Dispenses.ToString(CultureInfo.InvariantCulture))),
            "top-medicines" => dashboard.TopMedicines.Select(x => string.Join(',',
                EscapeCsv(x.MedicineName),
                x.QuantitySold.ToString(CultureInfo.InvariantCulture),
                x.Revenue.ToString("0.00", CultureInfo.InvariantCulture))),
            "stock-valuation" => dashboard.StockValuationItems.Select(x => string.Join(',',
                EscapeCsv(x.MedicineName),
                x.StockOnHand.ToString(CultureInfo.InvariantCulture),
                x.UnitCostPrice.ToString("0.00", CultureInfo.InvariantCulture),
                x.StockValue.ToString("0.00", CultureInfo.InvariantCulture),
                x.NearestExpiryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty)),
            "expiry" => dashboard.ExpiryItems.Select(x => string.Join(',',
                EscapeCsv(x.MedicineName),
                EscapeCsv(x.BatchNumber),
                x.ExpiryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x.QuantityOnHand.ToString(CultureInfo.InvariantCulture),
                x.DaysToExpiry.ToString(CultureInfo.InvariantCulture),
                x.PotentialLoss.ToString("0.00", CultureInfo.InvariantCulture),
                x.Severity)),
            "reconciliation" => dashboard.ReconciliationItems.Select(x => string.Join(',',
                x.PaymentNumber,
                x.Gateway,
                x.PaymentStatus,
                x.PaidAmount.ToString("0.00", CultureInfo.InvariantCulture),
                x.RefundedAmount.ToString("0.00", CultureInfo.InvariantCulture),
                x.NetAmount.ToString("0.00", CultureInfo.InvariantCulture),
                EscapeCsv(x.OrderNumber ?? string.Empty),
                EscapeCsv(x.InvoiceNumber ?? string.Empty),
                x.ReconciliationStatus)),
            _ => ["Unsupported report type"]
        };

        var header = reportType switch
        {
            "sales" => "Date,Revenue,Orders,Dispenses",
            "top-medicines" => "Medicine,QuantitySold,Revenue",
            "stock-valuation" => "Medicine,StockOnHand,UnitCostPrice,StockValue,NearestExpiryDate",
            "expiry" => "Medicine,BatchNumber,ExpiryDate,QuantityOnHand,DaysToExpiry,PotentialLoss,Severity",
            "reconciliation" => "PaymentNumber,Gateway,PaymentStatus,PaidAmount,RefundedAmount,NetAmount,OrderNumber,InvoiceNumber,ReconciliationStatus",
            _ => "Report"
        };

        return Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, [header, .. rows]));
    }

    private static byte[] BuildPdf(string reportType, PharmacyReportsDashboardResponse dashboard)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(column =>
                {
                    column.Item().Text("HealthCareMS Pharmacy Report").FontSize(18).Bold().FontColor(Colors.Green.Darken2);
                    column.Item().Text($"{reportType.ToUpperInvariant()} | {dashboard.From:yyyy-MM-dd} to {dashboard.To:yyyy-MM-dd}");
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Total Revenue: {dashboard.TotalRevenue:0.00} PKR").Bold();
                    column.Item().Text($"Total Refunded: {dashboard.TotalRefunded:0.00} PKR");
                    column.Item().Text($"Orders: {dashboard.TotalOrders} | Dispenses: {dashboard.TotalDispenses} | Stock Valuation: {dashboard.StockValuation:0.00} PKR");

                    var lines = reportType switch
                    {
                        "sales" => dashboard.SalesByPeriod.Select(x => $"{x.Date:yyyy-MM-dd} | Revenue {x.Revenue:0.00} | Orders {x.Orders} | Dispenses {x.Dispenses}"),
                        "top-medicines" => dashboard.TopMedicines.Select(x => $"{x.MedicineName} | Qty {x.QuantitySold} | Revenue {x.Revenue:0.00}"),
                        "stock-valuation" => dashboard.StockValuationItems.Select(x => $"{x.MedicineName} | Stock {x.StockOnHand} | Value {x.StockValue:0.00}"),
                        "expiry" => dashboard.ExpiryItems.Select(x => $"{x.MedicineName} | {x.BatchNumber} | {x.ExpiryDate:yyyy-MM-dd} | {x.DaysToExpiry} days"),
                        "reconciliation" => dashboard.ReconciliationItems.Select(x => $"{x.PaymentNumber} | {x.Gateway} | {x.PaymentStatus} | Net {x.NetAmount:0.00} | {x.ReconciliationStatus}"),
                        _ => ["Unsupported report type"]
                    };

                    foreach (var line in lines)
                    {
                        column.Item().Text(line);
                    }
                });
            });
        }).GeneratePdf();
    }

    private static string EscapeCsv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
