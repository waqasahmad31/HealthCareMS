namespace HealthCareMS.Infrastructure.Configuration;

public sealed class ApplicationLinkBuilder(ApplicationLinkOptions options) : IApplicationLinkBuilder
{
    public string BuildConsultationWaitingRoomUrl(Guid appointmentId)
    {
        var baseUrl = RequireBaseUrl(options.ClientBaseUrl, $"{ApplicationLinkOptions.SectionName}:ClientBaseUrl");
        var path = (options.ConsultationWaitingRoomPathTemplate ?? string.Empty)
            .Replace("{AppointmentId}", appointmentId.ToString(), StringComparison.OrdinalIgnoreCase);

        return Combine(baseUrl, path);
    }

    public string BuildPrescriptionVerificationUrl(Guid prescriptionId, string verificationCode)
    {
        var baseUrl = RequireBaseUrl(options.ApiBaseUrl, $"{ApplicationLinkOptions.SectionName}:ApiBaseUrl");
        var path = (options.PrescriptionVerificationPathTemplate ?? string.Empty)
            .Replace("{PrescriptionId}", prescriptionId.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{VerificationCode}", Uri.EscapeDataString(verificationCode), StringComparison.OrdinalIgnoreCase);

        return Combine(baseUrl, path);
    }

    private static string RequireBaseUrl(string? baseUrl, string key)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException($"{key} configuration is required.");
        }

        return baseUrl.TrimEnd('/');
    }

    private static string Combine(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return baseUrl;
        }

        return path.StartsWith('/', StringComparison.Ordinal)
            ? $"{baseUrl}{path}"
            : $"{baseUrl}/{path}";
    }
}
