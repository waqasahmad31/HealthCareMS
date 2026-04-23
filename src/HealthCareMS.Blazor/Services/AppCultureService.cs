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
        var stored = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        CurrentCulture = Normalize(stored);
        ApplyCulture(CurrentCulture);
    }

    public async Task SetCultureAsync(string culture)
    {
        CurrentCulture = Normalize(culture);
        ApplyCulture(CurrentCulture);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, CurrentCulture);
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
