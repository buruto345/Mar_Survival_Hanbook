using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public sealed class AuthenticationHttpTests(DiscoveryHttpFixture fixture) : IClassFixture<DiscoveryHttpFixture>
{
    private static RegisterRequest NewAccount() => new() {
        Username = "reader_" + Guid.NewGuid().ToString("N"), FullName = "  Nguyễn Văn An  ",
        Email = Guid.NewGuid().ToString("N") + "@example.test", Password = Guid.NewGuid().ToString("N") + "!"
    };
    private Task<HttpResponseMessage> Register(object request) => fixture.Guest.PostAsJsonAsync("/api/auth/register", request, DiscoveryJson.Options);
    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode status)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    [SqlServerFact]
    public async Task OperatorCanProvisionAdminsWithoutProfileWhilePublicRegistrationStillRequiresFullDetails()
    {
        using var scope = fixture.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
        var username = "operator_" + Guid.NewGuid().ToString("N");
        var request = new RegisterRequest { Username = username, Password = "short" };
        var profile = await auth.ProvisionAsync(request, UserRole.Admin, default);
        Assert.Null(profile.Email);
        Assert.Null(profile.FullName);
        Assert.Equal(UserRole.Admin, profile.Role);
        // Multiple admins may omit email; NULL must not collide in the unique index.
        await auth.ProvisionAsync(new RegisterRequest { Username = "operator_" + Guid.NewGuid().ToString("N"), Password = "short" }, UserRole.Admin, default);
        await using var db = fixture.Database.CreateContext();
        var saved = await db.Users.SingleAsync(x => x.Id == profile.Id);
        Assert.Null(saved.NormalizedEmail);
        Assert.NotEqual(request.Password, saved.PasswordHash);
        var login = await Json(await fixture.Guest.PostAsJsonAsync("/api/auth/login",
            new { username, password = request.Password }), HttpStatusCode.OK);
        Assert.Equal(JsonValueKind.Null, login.GetProperty("profile").GetProperty("email").ValueKind);
        Assert.Equal(JsonValueKind.Null, login.GetProperty("profile").GetProperty("fullName").ValueKind);
        using var sources = new HttpRequestMessage(HttpMethod.Get, "/api/admin/discover-mars/sources");
        sources.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await fixture.Guest.SendAsync(sources)).StatusCode);
        await Json(await Register(new { username = "public_" + Guid.NewGuid().ToString("N"), password = "short" }), HttpStatusCode.BadRequest);
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() => auth.ProvisionAsync(request, UserRole.Viewer, default));
    }

    [SqlServerFact]
    public async Task RegistrationStoresHashedPasswordAndViewerProfileCanLoginAndReadOwnProfile()
    {
        var input = NewAccount();
        var response = await Register(input);
        var profile = await Json(response, HttpStatusCode.Created);
        Assert.Equal("/api/auth/me", response.Headers.Location?.AbsolutePath);
        Assert.Equal(input.Username, profile.GetProperty("username").GetString());
        Assert.Equal("Nguyễn Văn An", profile.GetProperty("fullName").GetString());
        Assert.Equal("Viewer", profile.GetProperty("role").GetString());
        Assert.False(profile.TryGetProperty("password", out _));
        Assert.False(profile.TryGetProperty("passwordHash", out _));
        Assert.False(profile.TryGetProperty("normalizedEmail", out _));
        Assert.False(profile.TryGetProperty("displayName", out _));
        await using var db = fixture.Database.CreateContext();
        var saved = await db.Users.SingleAsync(x => x.Id == profile.GetProperty("id").GetGuid());
        Assert.Equal(UserRole.Viewer, saved.Role); Assert.Equal(UserState.Active, saved.State);
        Assert.Equal(input.Username.ToUpperInvariant(), saved.NormalizedUsername);
        Assert.Equal(input.Email.ToUpperInvariant(), saved.NormalizedEmail);
        Assert.NotEqual(input.Password, saved.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, input.Password));
        var login = await Json(await fixture.Guest.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(input.Email.ToUpperInvariant(), input.Password)), HttpStatusCode.OK);
        Assert.Equal(profile.GetRawText(), login.GetProperty("profile").GetRawText());
        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        Assert.Equal(profile.GetRawText(), (await Json(await fixture.Guest.SendAsync(me), HttpStatusCode.OK)).GetRawText());
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Guest.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Admin.GetAsync("/api/auth/me")).StatusCode);
    }

    [SqlServerFact]
    public async Task LoginAcceptsEmailOrUsernameAndRejectsInvalidCredentialsAndAmbiguousInputs()
    {
        var input = NewAccount();
        var profile = await Json(await Register(input), HttpStatusCode.Created);
        foreach (var credentials in new object[] {
            new { email = "  " + input.Email.ToUpperInvariant() + "  ", password = input.Password },
            new { username = "  " + input.Username.ToUpperInvariant() + "  ", password = input.Password }
        })
        {
            var login = await Json(await fixture.Guest.PostAsJsonAsync("/api/auth/login", credentials), HttpStatusCode.OK);
            Assert.Equal(profile.GetProperty("id").GetGuid(), login.GetProperty("profile").GetProperty("id").GetGuid());
            using var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
            me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
            Assert.Equal(profile.GetProperty("id").GetGuid(), (await Json(await fixture.Guest.SendAsync(me), HttpStatusCode.OK)).GetProperty("id").GetGuid());
        }
        foreach (var credentials in new object[] {
            new { username = input.Username, password = "wrong-password" },
            new { username = "missing_" + Guid.NewGuid().ToString("N"), password = input.Password },
            new { email = input.Email, password = "wrong-password" }
        })
            await Json(await fixture.Guest.PostAsJsonAsync("/api/auth/login", credentials), HttpStatusCode.Unauthorized);
        foreach (var credentials in new object[] {
            new { password = input.Password },
            new { username = "   ", password = input.Password },
            new { email = input.Email, username = input.Username, password = input.Password },
            new { email = "invalid", password = input.Password },
            new { username = input.Username },
            new { username = new string('x', 51), password = input.Password }
        })
            await Json(await fixture.Guest.PostAsJsonAsync("/api/auth/login", credentials), HttpStatusCode.BadRequest);

        await using var db = fixture.Database.CreateContext();
        var user = await db.Users.SingleAsync(x => x.Username == input.Username);
        foreach (var state in new[] { UserState.Suspended, UserState.Deactivated, UserState.Active })
        {
            user.State = state;
            user.ArchivedAt = state == UserState.Active ? DateTimeOffset.UtcNow : null;
            await db.SaveChangesAsync();
            await Json(await fixture.Guest.PostAsJsonAsync("/api/auth/login",
                new { username = input.Username, password = input.Password }), HttpStatusCode.Unauthorized);
        }
    }

    [SqlServerFact]
    public async Task RegistrationRejectsMissingInvalidAndPrivilegeFieldsWithoutCreatingAccounts()
    {
        await using var db = fixture.Database.CreateContext();
        var before = await db.Users.CountAsync();
        var invalid = new (string Field, object? Value)[] {
            ("username", null), ("username", "ab"), ("username", new string('a', 51)),
            ("username", ".bad"), ("username", "white space"), ("username", "nguyễn"),
            ("fullName", null), ("fullName", "   "), ("fullName", new string('a', 151)),
            ("email", "invalid"), ("email", ""), ("password", "short"), ("password", new string('x', 129)),
            ("password", new string(' ', 12)), ("role", "Admin"), ("role", 1), ("state", "Active"),
            ("passwordHash", "injected"), ("normalizedUsername", "ADMIN")
        };
        foreach (var (field, value) in invalid)
        {
            var input = JsonSerializer.Deserialize<Dictionary<string, object?>>(DiscoveryJson.Write(NewAccount()))!;
            input[field] = value;
            await Json(await Register(input), HttpStatusCode.BadRequest);
        }
        await Json(await Register(new { email = "missing@example.test", password = "ValidPasswordLength123" }), HttpStatusCode.BadRequest);
        Assert.Equal(before, await db.Users.CountAsync());
    }

    [SqlServerFact]
    public async Task DuplicateUsernameAndEmailAreCaseInsensitiveAndConcurrentRegistrationCreatesOneAccount()
    {
        var input = NewAccount();
        await Json(await Register(input), HttpStatusCode.Created);
        var duplicateName = NewAccount(); duplicateName.Username = input.Username.ToUpperInvariant();
        await Json(await Register(duplicateName), HttpStatusCode.Conflict);
        var duplicateEmail = NewAccount(); duplicateEmail.Email = input.Email.ToUpperInvariant();
        await Json(await Register(duplicateEmail), HttpStatusCode.Conflict);
        var concurrent = NewAccount();
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Register(concurrent)));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Equal(3, responses.Count(x => x.StatusCode == HttpStatusCode.Conflict));
        await using var db = fixture.Database.CreateContext();
        Assert.Equal(1, await db.Users.CountAsync(x => x.NormalizedUsername == concurrent.Username.ToUpperInvariant()));
    }

    [SqlServerFact]
    public async Task ViewerCanReadPublicContentButEveryCmsEndpointRejectsViewer()
    {
        Assert.Equal(HttpStatusCode.OK, (await fixture.Viewer.GetAsync("/api/discover-mars?language=en")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Viewer.GetAsync("/api/discover-mars/articles?language=en")).StatusCode);
        var endpoints = fixture.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(x => x.RoutePattern.RawText?.TrimStart('/').StartsWith("api/admin/", StringComparison.Ordinal) == true).ToArray();
        Assert.True(endpoints.Length > 50, "Expected the complete CMS surface, including mutations, imports, sources and reviews.");
        foreach (var endpoint in endpoints)
        {
            var route = Regex.Replace(endpoint.RoutePattern.RawText!, @"\{[^}]+\}", Guid.NewGuid().ToString());
            foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), "/" + route.TrimStart('/'));
                if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { });
                using var response = await fixture.Viewer.SendAsync(request);
                Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"Viewer reached {method} {route}: {response.StatusCode}");
            }
        }
    }

    [SqlServerFact]
    public async Task SuspendedArchivedAndDeactivatedServiceAccountsCannotLogin()
    {
        var input = NewAccount();
        await Json(await Register(input), HttpStatusCode.Created);
        await using var db = fixture.Database.CreateContext();
        var saved = await db.Users.SingleAsync(x => x.Username == input.Username);
        foreach (var state in new[] { UserState.Suspended, UserState.Deactivated })
        {
            saved.State = state;
            await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Guest.PostAsJsonAsync("/api/auth/login", new LoginRequest(input.Email, input.Password))).StatusCode);
        }
        saved.State = UserState.Active; saved.ArchivedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Guest.PostAsJsonAsync("/api/auth/login", new LoginRequest(input.Email, input.Password))).StatusCode);
        saved.State = UserState.Deactivated; saved.ArchivedAt = null; saved.PasswordHash = "!disabled-no-interactive-login";
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Guest.PostAsJsonAsync("/api/auth/login", new LoginRequest(input.Email, input.Password))).StatusCode);
    }

    [SqlServerFact]
    public async Task RegistrationRateLimitReturns429()
    {
        var limited = new DiscoveryHttpFixture { RegistrationPermitLimit = 2 };
        await limited.InitializeAsync();
        try
        {
            for (var i = 0; i < 2; i++)
                Assert.Equal(HttpStatusCode.BadRequest, (await limited.Guest.PostAsJsonAsync("/api/auth/register", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await limited.Guest.PostAsJsonAsync("/api/auth/register", new { })).StatusCode);
        }
        finally { await limited.DisposeAsync(); }
    }
}
