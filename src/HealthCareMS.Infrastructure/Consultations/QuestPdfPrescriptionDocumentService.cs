using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Consultations;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HealthCareMS.Infrastructure.Consultations;

public sealed class QuestPdfPrescriptionDocumentService : IPrescriptionDocumentService
{
    public QuestPdfPrescriptionDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePrescriptionPdf(Prescription prescription, string verificationUrl)
    {
        var qrPng = GenerateQrCode(verificationUrl);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("HealthCareMS e-Prescription").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            left.Item().Text($"Prescription: {prescription.PrescriptionNumber}").FontSize(11);
                            left.Item().Text($"Issued: {prescription.IssuedAt:yyyy-MM-dd HH:mm} UTC");
                        });

                        row.ConstantItem(92).Image(qrPng).FitArea();
                    });

                    column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Patient").Bold();
                            left.Item().Text($"{prescription.Patient.FirstName} {prescription.Patient.LastName}".Trim());
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Doctor").Bold();
                            right.Item().Text(prescription.Doctor.User.FullName);
                            right.Item().Text($"PMDC: {prescription.Doctor.PmdcRegistrationNumber}");
                        });
                    });

                    column.Item().Text("Diagnosis").Bold();
                    column.Item().Text(prescription.Diagnosis);
                    if (!string.IsNullOrWhiteSpace(prescription.Icd10Code))
                    {
                        column.Item().Text($"ICD-10: {prescription.Icd10Code} - {prescription.Icd10Title}");
                    }

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Medicine");
                            header.Cell().Element(HeaderCell).Text("Dose");
                            header.Cell().Element(HeaderCell).Text("Frequency");
                            header.Cell().Element(HeaderCell).Text("Duration");
                            header.Cell().Element(HeaderCell).Text("Qty");
                        });

                        foreach (var item in prescription.Items.OrderBy(x => x.SortOrder))
                        {
                            table.Cell().Element(BodyCell).Text($"{item.MedicineName} {item.Strength}".Trim());
                            table.Cell().Element(BodyCell).Text(item.Dosage);
                            table.Cell().Element(BodyCell).Text(item.Frequency);
                            table.Cell().Element(BodyCell).Text($"{item.DurationDays} days");
                            table.Cell().Element(BodyCell).Text(item.Quantity.ToString("0.##"));
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(prescription.ClinicalNotes))
                    {
                        column.Item().Text("Clinical Notes").Bold();
                        column.Item().Text(prescription.ClinicalNotes);
                    }

                    column.Item().PaddingTop(14).Column(signature =>
                    {
                        signature.Item().Text($"Digitally signed by {prescription.Doctor.User.FullName}").Bold();
                        signature.Item().Text($"Signature: {prescription.DigitalSignature}");
                        signature.Item().Text($"Verify: {verificationUrl}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Prescription valid until ");
                    text.Span(prescription.ValidUntil.ToString("yyyy-MM-dd")).Bold();
                    text.Span(" | QR verification required for authenticity.");
                });
            });
        }).GeneratePdf();
    }

    private static byte[] GenerateQrCode(string verificationUrl)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(8);
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
