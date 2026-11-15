using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MarsHandbook.Client;
using MarsHandbook.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API backend (Render). Dev: goi thang Render (CORS cho phep http://localhost:5173).
// Production: de trong -> cung origin, host chuyen tiep /api/* (xem wwwroot/_redirects).
var apiBase = builder.Configuration["ApiBaseUrl"];
var apiUri = string.IsNullOrWhiteSpace(apiBase)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri(apiBase.TrimEnd('/') + "/");

builder.Services.AddScoped(sp => new MarsApi(new HttpClient { BaseAddress = apiUri, Timeout = TimeSpan.FromSeconds(75) }, apiUri));
builder.Services.AddScoped<MapInterop>();

await builder.Build().RunAsync();
