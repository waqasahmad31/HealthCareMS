using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HealthCareMS.Blazor;
using HealthCareMS.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["ApiBaseAddress"];
if (string.IsNullOrWhiteSpace(apiBaseAddress))
{
    throw new InvalidOperationException("Configuration key 'ApiBaseAddress' is required.");
}

var apiEndpoints = new ApiEndpointOptions(apiBaseAddress);

builder.Services.AddSingleton(apiEndpoints);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiEndpoints.BaseUri });
builder.Services.AddScoped<HealthCareApiClient>();

await builder.Build().RunAsync();
