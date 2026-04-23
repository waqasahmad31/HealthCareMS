using System.Net.Http.Headers;
using System.Net.Http.Json;
using HealthCareMS.Shared.Api;
using Microsoft.JSInterop;

namespace HealthCareMS.Blazor.Services;

public sealed class HealthCareApiClient(
    HttpClient httpClient,
    IJSRuntime jsRuntime,
    AuthSessionService authSession)
{
    public async Task<ApiResponse<T>?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Get, url, content: null, cancellationToken);
    }

    public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync<TResponse>(HttpMethod.Post, url, JsonContent.Create(payload), cancellationToken);
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Post, url, content: null, cancellationToken);
    }

    public async Task<ApiResponse<T>?> PutAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Put, url, content: null, cancellationToken);
    }

    public async Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(
        string url,
        TRequest payload,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync<TResponse>(HttpMethod.Put, url, JsonContent.Create(payload), cancellationToken);
    }

    public async Task<ApiResponse<T>?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Delete, url, content: null, cancellationToken);
    }

    public async Task<ApiResponse<T>?> PostMultipartAsync<T>(
        string url,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Post, url, content, cancellationToken);
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
        var token = await authSession.GetValidAccessTokenAsync(allowRefresh: true);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var culture = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "HealthCareMS.Culture");
        if (!string.IsNullOrWhiteSpace(culture))
        {
            request.Headers.AcceptLanguage.Clear();
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(
                string.Equals(culture, "ur", StringComparison.OrdinalIgnoreCase) ? "ur" : "en"));
        }
    }

    private static ApiResponse<T> Fail<T>(string code, string message)
    {
        return ApiResponse<T>.Fail(
            new Shared.Common.Error(code, message),
            Guid.NewGuid().ToString("N"));
    }

    private async Task<ApiResponse<T>> SendAsync<T>(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url)
            {
                Content = content
            };
            await AddAuthorizationAsync(request);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                {
                    await authSession.ClearSessionAsync();
                }

                return Fail<T>(
                    $"HTTP_{(int)response.StatusCode}",
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken);
            return apiResponse ?? Fail<T>("CLIENT_EMPTY_RESPONSE", "The server returned an empty response.");
        }
        catch (HttpRequestException ex)
        {
            return Fail<T>("CLIENT_NETWORK_ERROR", $"Unable to reach API endpoint: {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            return Fail<T>("CLIENT_RESPONSE_UNSUPPORTED", $"Unsupported API response format: {ex.Message}");
        }
        catch (System.Text.Json.JsonException ex)
        {
            return Fail<T>("CLIENT_RESPONSE_PARSE_ERROR", $"Unable to parse API response: {ex.Message}");
        }
    }
}

public sealed record FileDownloadResult(
    bool Success,
    string FileName,
    string ContentType,
    byte[] Content,
    string? ErrorMessage);
