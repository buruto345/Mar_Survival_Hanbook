using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class DiscoveryHttpFixture : IAsyncLifetime
{
    public SqlServerFixture Database { get; } = new();
    private WebApplicationFactory<Program>? factory;
    public HttpClient Guest { get; private set; } = null!;
    public HttpClient Viewer { get; private set; } = null!;
    public HttpClient OtherAdmin { get; private set; } = null!;
    public HttpClient Admin { get; private set; } = null!;
    public Guid AdminId { get; private set; }
    public int RegistrationPermitLimit { get; init; } = 1000;
    public IServiceProvider Services => factory!.Services;
    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("MARS_RUN_SQLSERVER_TESTS") != "1") return;
        await Database.InitializeAsync();
        await using var db = Database.CreateContext();
        await DiscoverySeed.RunAsync(db);
        var password = Guid.NewGuid().ToString("N") + "!aA1";
        foreach (var (username, role) in new[] { ("viewer", UserRole.Viewer), ("otheradmin", UserRole.Admin), ("admin", UserRole.Admin) })
        {
            var email = username + "@discovery.test";
            var u = new User { Username = username, NormalizedUsername = username.ToUpperInvariant(),
                Email = email, NormalizedEmail = email.ToUpperInvariant(), FullName = username, Role = role };
            u.PasswordHash = new PasswordHasher<User>().HashPassword(u, password); db.Users.Add(u);
            if (username == "admin") AdminId = u.Id;
        }
        await db.SaveChangesAsync();
        var cs = db.Database.GetConnectionString()!;
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:MarsDatabase"] = cs, ["Nasa:WorkerEnabled"] = "false",
                ["Auth:LoginPermitLimit"] = "1000",
                ["Auth:RegistrationPermitLimit"] = RegistrationPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Storage:RawRoot"] = Path.Combine(Path.GetTempPath(), "MarsDiscoveryTests", Guid.NewGuid().ToString("N")),
                ["Nasa:AllowLocalSimulation"] = "true", ["Nasa:SimulationPort"] = "4010", ["Nasa:TimeoutSeconds"] = "1",
                ["Logging:LogLevel:Default"] = "Warning", ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
            }));
        });
        Guest = factory.CreateClient();
        async Task<HttpClient> Login(string username)
        {
            var client = factory.CreateClient();
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username + "@discovery.test", password), DiscoveryJson.Options);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); return client;
        }
        Viewer = await Login("viewer"); OtherAdmin = await Login("otheradmin"); Admin = await Login("admin");
    }
    public async Task DisposeAsync()
    {
        Guest?.Dispose(); Viewer?.Dispose(); OtherAdmin?.Dispose(); Admin?.Dispose();
        if (factory != null) await factory.DisposeAsync();
        await Database.DisposeAsync();
    }
}
