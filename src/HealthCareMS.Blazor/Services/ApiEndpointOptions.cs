namespace HealthCareMS.Blazor.Services;

public sealed record ApiEndpointOptions(string BaseAddress)
{
    public Uri BaseUri => new(BaseAddress.EndsWith("/", StringComparison.Ordinal) ? BaseAddress : $"{BaseAddress}/");

    public string QueueHubUrl => new Uri(BaseUri, "hubs/queue").ToString();

    public string NotificationHubUrl => new Uri(BaseUri, "hubs/notifications").ToString();

    public string ConsultationHubUrl => new Uri(BaseUri, "hubs/consultations").ToString();

    public string ConsultationChatHubUrl => new Uri(BaseUri, "hubs/consultation-chat").ToString();
}
