using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using HealthCareMS.Blazor;
using HealthCareMS.Blazor.Services;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress =
    builder.Configuration["ApplicationLinks:ApiBaseUrl"]
    ?? builder.Configuration["ApiBaseAddress"];
if (string.IsNullOrWhiteSpace(apiBaseAddress))
{
    throw new InvalidOperationException("Configuration key 'ApplicationLinks:ApiBaseUrl' is required.");
}

var apiEndpoints = new ApiEndpointOptions(apiBaseAddress);

builder.Services.AddSingleton(apiEndpoints);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiEndpoints.BaseUri });
builder.Services.AddScoped<AuthSessionService>();
builder.Services.AddScoped<HealthCareApiClient>();
builder.Services.AddScoped<AppCultureService>();

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en");

await builder.Build().RunAsync();
