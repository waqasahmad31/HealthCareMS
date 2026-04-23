using HealthCareMS.Application.Consultations;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HealthCareMS.Infrastructure.Consultations;

public sealed class QuestPdfConsultationSummaryDocumentService : IConsultationSummaryDocumentService
{
    public QuestPdfConsultationSummaryDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateSummaryPdf(ConsultationSummaryResponse summary)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(column =>
                {
                    column.Item().Text("HealthCareMS Consultation Summary").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"{summary.AppointmentNumber} | {summary.Status}");
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Patient").Bold();
                            left.Item().Text(summary.PatientName);
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Doctor").Bold();
                            right.Item().Text(summary.DoctorName);
                        });
                    });

                    column.Item().Text("Diagnosis").Bold();
                    column.Item().Text(summary.Diagnosis ?? "-");
                    if (!string.IsNullOrWhiteSpace(summary.Icd10Code))
                    {
                        column.Item().Text($"ICD-10: {summary.Icd10Code} - {summary.Icd10Title}");
                    }

                    if (!string.IsNullOrWhiteSpace(summary.ClinicalNotes))
                    {
                        column.Item().Text("Clinical Notes").Bold();
                        column.Item().Text(summary.ClinicalNotes);
                    }

                    column.Item().Text("Prescription").Bold();
                    if (summary.Prescription is null || summary.Prescription.Items.Count == 0)
                    {
                        column.Item().Text("No prescription recorded.");
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Medicine");
                                header.Cell().Element(HeaderCell).Text("Dose");
                                header.Cell().Element(HeaderCell).Text("Duration");
                            });
                            foreach (var item in summary.Prescription.Items)
                            {
                                table.Cell().Element(BodyCell).Text(item.MedicineName);
                                table.Cell().Element(BodyCell).Text($"{item.Dosage} {item.Frequency}");
                                table.Cell().Element(BodyCell).Text($"{item.DurationDays} days");
                            }
                        });
                    }

                    column.Item().Text("Lab Orders").Bold();
                    if (summary.LabOrders.Count == 0)
                    {
                        column.Item().Text("No lab orders recorded.");
                    }
                    else
                    {
                        foreach (var order in summary.LabOrders)
                        {
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(orderColumn =>
                            {
                                orderColumn.Item().Text($"{order.BookingNumber} | {order.CollectionType} | {order.Status}").Bold();
                                orderColumn.Item().Text($"Total: PKR {order.TotalAmount:0.##}");
                                foreach (var item in order.Items)
                                {
                                    orderColumn.Item().Text($"{item.TestCode} - {item.TestName}");
                                }
                            });
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ");
                    text.Span(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'")).Bold();
                });
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
}
