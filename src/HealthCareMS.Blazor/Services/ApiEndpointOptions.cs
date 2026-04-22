namespace HealthCareMS.Blazor.Services;

public sealed record ApiEndpointOptions(string BaseAddress)
{
    public Uri BaseUri => new(BaseAddress.EndsWith("/", StringComparison.Ordinal) ? BaseAddress : $"{BaseAddress}/");

    public string QueueHubUrl => new Uri(BaseUri, "hubs/queue").ToString();
}
