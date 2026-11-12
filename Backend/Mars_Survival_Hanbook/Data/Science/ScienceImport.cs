using System.Security.Claims;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Science;

public static class ScienceImport
{
    private sealed record Entry(string Slug, string Category, string Title, TestEnvironment Environment, TechnologyStatus Status,
        string Problem, string Principle, string Capabilities, string Limitations, string SourceTitle, string Url, string Locator);

    public static async Task RunAsync(IServiceProvider services, string? file = null)
    {
        var entries = DiscoveryJson.Read<Entry[]>(await File.ReadAllTextAsync(file ?? Path.Combine(AppContext.BaseDirectory, "Data", "Science", "release.en.json")));
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<MarsDbContext>();
        await DiscoverySeed.RunAsync(db);
        const string username = "science-import";
        await using var tx = await db.Database.BeginTransactionAsync();
        await DatabaseLocks.AcquireAsync(db, username, default);
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedUsername == "SCIENCE-IMPORT");
        if (user == null)
        {
            user = new User { Username = username, NormalizedUsername = "SCIENCE-IMPORT",
                FullName = "Science catalog import", Role = UserRole.Admin,
                State = UserState.Deactivated, PasswordHash = "!disabled-science-import" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        if (user.State != UserState.Deactivated || user.Role != UserRole.Admin || user.PasswordHash != "!disabled-science-import")
            throw new InvalidOperationException("Reserved local release identity has changed.");
        await tx.CommitAsync();

        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, nameof(UserRole.Admin))
        ], "LocalContentRelease")) };
        try
        {
            var articles = ActivatorUtilities.CreateInstance<ArticleService>(sp, new ContentModule(ContentModule.Science));
            var sources = sp.GetRequiredService<SourceService>();
            foreach (var e in entries)
            {
                var marker = "Science pack: " + SourceNormalization.Hash(DiscoveryJson.Write(e));
                var existing = await db.Articles.SingleOrDefaultAsync(a => a.Slug == e.Slug);
                if (existing != null)
                {
                    var current = await articles.VersionAsync(existing.Id, existing.CurrentDraftVersionId!.Value, default);
                    if (existing.CreatedById != user.Id || current.ChangedById != user.Id || current.ChangeSummary?.StartsWith("Science pack: ") != true)
                        throw ApiException.Conflict("Manual changes require editorial reconciliation: " + e.Slug);
                    if (current.ChangeSummary == marker) continue;
                }
                var revision = (await sources.ManualAsync(new(e.Url, e.SourceTitle, [new("NASA", "NASA")], "NASA",
                    SourceType.WebPage, e.Principle, "Paraphrase. Source location: " + e.Locator,
                    DateTimeOffset.UtcNow, "NASA"), default)).Revision;
                var tech = await db.Technologies.SingleOrDefaultAsync(t => t.Slug == e.Slug);
                if (tech == null)
                {
                    tech = new Technology { Slug = e.Slug, Name = e.Title, Description = e.Principle,
                        Capabilities = e.Capabilities, Limitations = e.Limitations, Status = e.Status,
                        EnvironmentTested = e.Environment, EvidenceType = EvidenceType.Tested };
                    db.Add(tech);
                    await db.SaveChangesAsync();
                }
                var draft = new ArticleDraftRequest { Slug = e.Slug, Title = e.Title, Summary = e.Capabilities, CategorySlug = e.Category,
                    ChangeSummary = marker, Sections = [new("problem", "Problem", e.Problem), new("principle", "How it works", e.Principle)],
                    Claims = [new("principle", e.Principle, EvidenceType.Tested, e.Environment, [revision.Id], new() { ["section"] = e.Locator })],
                    TechnologyProfile = new(tech.Id, e.Status, EvidenceType.Tested, e.Environment, e.Capabilities, e.Limitations, [revision.Id], [], []) };
                var article = await articles.SaveDraftAsync(existing?.Id, draft, existing == null ? null : Concurrency.ETag(existing), default);
                Console.WriteLine($"{e.Slug}: draft {article.CurrentDraftVersionId}; review source {revision.Id}");
            }
        }
        finally { accessor.HttpContext = previous; }
    }
}
