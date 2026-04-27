using System.Globalization;
using Microsoft.JSInterop;

namespace HealthCareMS.Blazor.Services;

public sealed class AppCultureService(IJSRuntime jsRuntime)
{
    private const string StorageKey = "HealthCareMS.Culture";

    public string CurrentCulture { get; private set; } = "en";

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        try
        {
            CurrentCulture = Normalize(await jsRuntime.InvokeAsync<string>("healthCareCulture.init"));
        }
        catch (JSException)
        {
            CurrentCulture = "en";
        }

        ApplyCulture(CurrentCulture);
    }

    public async Task SetCultureAsync(string culture)
    {
        var normalized = Normalize(culture);
        try
        {
            CurrentCulture = Normalize(await jsRuntime.InvokeAsync<string>("healthCareCulture.apply", normalized));
        }
        catch (JSException)
        {
            CurrentCulture = normalized;
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, CurrentCulture);
        }

        ApplyCulture(CurrentCulture);
        Changed?.Invoke();
    }

    private static void ApplyCulture(string culture)
    {
        var info = new CultureInfo(culture);
        CultureInfo.DefaultThreadCurrentCulture = info;
        CultureInfo.DefaultThreadCurrentUICulture = info;
    }

    private static string Normalize(string? culture)
    {
        return string.Equals(culture, "ur", StringComparison.OrdinalIgnoreCase) ? "ur" : "en";
    }
}
