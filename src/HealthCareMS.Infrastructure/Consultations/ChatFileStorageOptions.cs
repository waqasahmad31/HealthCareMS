namespace HealthCareMS.Infrastructure.Consultations;

public sealed class ChatFileStorageOptions
{
    public const string SectionName = "ChatFileStorage";

    public string RootPath { get; set; } = "storage/ConsultationChat";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}
