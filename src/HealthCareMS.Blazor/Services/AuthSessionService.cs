using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HealthCareMS.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HealthCareMS.Blazor.Services;

public sealed class AuthSessionService(HttpClient httpClient, IJSRuntime jsRuntime, NavigationManager navigation)
{
    private const string AccessTokenKey = "HealthCareMS.AccessToken";
    private const string RefreshTokenKey = "HealthCareMS.RefreshToken";
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(30);

    private bool isRefreshing;

    public async Task<bool> EnsureRouteAccessAsync()
    {
        var route = GetCurrentRelativeRoute();
        if (IsPublicRoute(route))
        {
            return true;
        }

        var token = await GetValidAccessTokenAsync(allowRefresh: true);
        if (!string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        RedirectToLogin(route);
        return false;
    }

    public async Task<string?> GetValidAccessTokenAsync(bool allowRefresh)
    {
        if (isRefreshing)
        {
            await WaitForRefreshAsync();
        }

        var accessToken = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        if (!IsTokenExpired(accessToken))
        {
            return accessToken;
        }

        if (!allowRefresh)
        {
            return null;
        }

        var refreshed = await TryRefreshAsync();
        return refreshed ? await jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey) : null;
    }

    public async Task StoreSessionAsync(string accessToken, string refreshToken)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }

    public async Task ClearSessionAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }

    public void RedirectToLogin(string? returnUrl = null)
    {
        var currentRoute = string.IsNullOrWhiteSpace(returnUrl) ? GetCurrentRelativeRoute() : returnUrl;
        if (IsPublicRoute(currentRoute))
        {
            navigation.NavigateTo("/login", forceLoad: true);
            return;
        }

        var destination = string.IsNullOrWhiteSpace(currentRoute)
            ? "/login"
            : $"/login?returnUrl={Uri.EscapeDataString(currentRoute.StartsWith("/", StringComparison.Ordinal) ? currentRoute : "/" + currentRoute)}";
        navigation.NavigateTo(destination, forceLoad: true);
    }

    public string ResolvePostLoginRoute()
    {
        var uri = navigation.ToAbsoluteUri(navigation.Uri);
        if (TryGetQueryParameter(uri.Query, "returnUrl", out var returnUrl))
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return returnUrl;
            }
        }

        return "/";
    }

    private async Task<bool> TryRefreshAsync()
    {
        if (isRefreshing)
        {
            return false;
        }

        var refreshToken = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await ClearSessionAsync();
            return false;
        }

        try
        {
            isRefreshing = true;
            var response = await httpClient.PostAsJsonAsync("api/v1/auth/refresh", new { refreshToken });
            if (!response.IsSuccessStatusCode)
            {
                await ClearSessionAsync();
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginRefreshPayload>>();
            if (payload?.Success != true || payload.Data is null)
            {
                await ClearSessionAsync();
                return false;
            }

            await StoreSessionAsync(payload.Data.AccessToken, payload.Data.RefreshToken);
            return true;
        }
        catch
        {
            await ClearSessionAsync();
            return false;
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private static bool IsPublicRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return false;
        }

        return route.StartsWith("login", StringComparison.OrdinalIgnoreCase);
    }

    private async Task WaitForRefreshAsync()
    {
        var attempts = 0;
        while (isRefreshing && attempts < 20)
        {
            attempts++;
            await Task.Delay(50);
        }
    }

    private string GetCurrentRelativeRoute()
    {
        var relative = navigation.ToBaseRelativePath(navigation.Uri);
        return string.IsNullOrWhiteSpace(relative) ? "/" : relative;
    }

    private static bool IsTokenExpired(string token)
    {
        var segments = token.Split('.');
        if (segments.Length < 2)
        {
            return true;
        }

        try
        {
            var payload = DecodeBase64Url(segments[1]);
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("exp", out var expElement))
            {
                return true;
            }

            var expSeconds = expElement.GetInt64();
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
            return expiresAt <= DateTime.UtcNow.Add(ExpirySkew);
        }
        catch
        {
            return true;
        }
    }

    private static string DecodeBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        if (normalized.Length % 4 == 2)
        {
            normalized += "==";
        }
        else if (normalized.Length % 4 == 3)
        {
            normalized += "=";
        }

        var bytes = Convert.FromBase64String(normalized);
        return Encoding.UTF8.GetString(bytes);
    }

    private static bool TryGetQueryParameter(string query, string key, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var raw = query.StartsWith('?') ? query[1..] : query;
        var parts = raw.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var split = part.Split('=', 2);
            if (!string.Equals(split[0], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = split.Length > 1 ? Uri.UnescapeDataString(split[1]) : string.Empty;
            return true;
        }

        return false;
    }

    private sealed record LoginRefreshPayload(
        string AccessToken,
        string RefreshToken,
        DateTimeOffset ExpiresAt);
}
