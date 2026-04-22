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

    public async Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(
        string url,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(payload)
        };
        await AddAuthorizationAsync(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(cancellationToken);
    }

    public async Task<ApiResponse<T>?> PostMultipartAsync<T>(
        string url,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        await AddAuthorizationAsync(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
    }

    public async Task<FileDownloadResult> DownloadFileAsync(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        await AddAuthorizationAsync(request);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new FileDownloadResult(false, string.Empty, string.Empty, [], "Download failed.");
        }

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "Attachment";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return new FileDownloadResult(true, fileName, contentType, bytes, null);
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

public sealed record FileDownloadResult(
    bool Success,
    string FileName,
    string ContentType,
    byte[] Content,
    string? ErrorMessage);
