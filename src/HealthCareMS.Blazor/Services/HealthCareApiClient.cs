using System.Net.Http.Headers;
using System.Net.Http.Json;
using HealthCareMS.Shared.Api;
using Microsoft.JSInterop;

namespace HealthCareMS.Blazor.Services;

public sealed class HealthCareApiClient(HttpClient httpClient, IJSRuntime jsRuntime)
{
    public async Task<ApiResponse<T>?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        await AddAuthorizationAsync(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }

    public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        await AddAuthorizationAsync(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(cancellationToken);
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        await AddAuthorizationAsync(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }

    public async Task<ApiResponse<T>?> PutAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        await AddAuthorizationAsync(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }

    private async Task AddAuthorizationAsync(HttpRequestMessage request)
    {
        var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "HealthCareMS.AccessToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
