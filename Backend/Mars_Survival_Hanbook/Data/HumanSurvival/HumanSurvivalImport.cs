using System.Security.Claims;
using Mars_Survival_Hanbook.Integrations.Nasa;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.HumanSurvival;

public static class HumanSurvivalImport
{
    private sealed record Source(string Key, string? NtrsId, string? Url, string? Title, string? Author, string? Organization);
    private sealed record Entry(string Slug, string Category, string Title, string Summary, string Scenario,
        string Question, string Explanation, string Limits, string Claim, string Source, string Locator,
        EvidenceType Evidence, TestEnvironment Environment);
    private sealed record Pack(string LanguageCode, Source[] Sources, Entry[] Articles);

    public static async Task RunAsync(IServiceProvider services, string? file = null)
    {
        var path = file ?? Path.Combine(AppContext.BaseDirectory, "Data", "HumanSurvival", "release.en.json");
        var pack = DiscoveryJson.Read<Pack>(await File.ReadAllTextAsync(path));
        if (pack.LanguageCode != "en") throw ApiException.Invalid("languageCode must be en.");
        var catalog = pack.Sources.ToDictionary(source => source.Key);
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<MarsDbContext>();
        await DiscoverySeed.RunAsync(db);
        const string username = "human-survival-import";
        await using var tx = await db.Database.BeginTransactionAsync();
        await DatabaseLocks.AcquireAsync(db, username, default);
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedUsername == "HUMAN-SURVIVAL-IMPORT");
        if (user == null)
        {
            user = new User { Username = username, NormalizedUsername = "HUMAN-SURVIVAL-IMPORT",
                FullName = "Human Survival import", Role = UserRole.Admin,
                State = UserState.Deactivated, PasswordHash = "!disabled-human-survival-import" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        if (user.State != UserState.Deactivated || user.Role != UserRole.Admin || user.PasswordHash != "!disabled-human-survival-import")
            throw new InvalidOperationException("Reserved local release identity has changed.");
        await tx.CommitAsync();

        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, nameof(UserRole.Admin))
        ], "LocalContentRelease")) };
        try
        {
            var articles = ActivatorUtilities.CreateInstance<ArticleService>(sp, new ContentModule(ContentModule.Human));
            var sources = sp.GetRequiredService<SourceService>();
            foreach (var entry in pack.Articles)
            {
                try
                {
                    var source = catalog[entry.Source];
                    var revision = await ResolveAsync(db, sources, sp.GetRequiredService<NasaConnectorClient>(), user.Id, source, entry);
                    var draft = new ArticleDraftRequest {
                        CategorySlug = entry.Category, Slug = entry.Slug, Title = entry.Title, Summary = entry.Summary,
                        LanguageCode = pack.LanguageCode, Sections = [
                            new("scenario", "Fictional scenario", entry.Scenario), new("question", "Your turn", entry.Question),
                            new("explanation", "One possible response", entry.Explanation),
                            new("evidence-limits", "Evidence and limits", entry.Claim + " " + entry.Limits)
                        ], Claims = [new("evidence-limits", entry.Claim, entry.Evidence, entry.Environment,
                            [revision.Id], new() { ["section"] = entry.Locator })]
                    };
                    const string prefix = "Human Survival pack ";
                    draft.ChangeSummary = prefix + SourceNormalization.Hash(DiscoveryJson.Write(draft));
                    var article = await db.Articles.SingleOrDefaultAsync(a => a.Slug == entry.Slug);
                    var current = article?.CurrentDraftVersionId is Guid id ? await articles.VersionAsync(article.Id, id, default) : null;
                    if (article != null && (article.CreatedById != user.Id || current?.ChangedById != user.Id ||
                        current.ChangeSummary?.StartsWith(prefix, StringComparison.Ordinal) != true))
                        throw ApiException.Conflict("Reconcile CMS edits before importing.");
                    if (current?.ChangeSummary != draft.ChangeSummary)
                        article = await articles.SaveDraftAsync(article?.Id, draft, article == null ? null : Concurrency.ETag(article), default);
                    var version = await articles.VersionAsync(article!.Id, article.CurrentDraftVersionId!.Value, default);
                    Console.WriteLine($"{entry.Slug} article={article.Id} version={version.Id} ({version.ReviewState}) source={revision.Id} ({revision.VerificationStatus})");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Import failed at article '{entry.Slug}', source '{entry.Source}': {ex.Message}", ex);
                }
            }
        }
        finally { accessor.HttpContext = previous; }
    }

    private static async Task<SourceRevision> ResolveAsync(MarsDbContext db, SourceService sources, NasaConnectorClient nasa,
        Guid actorId, Source source, Entry entry)
    {
        if (source.NtrsId is { } id)
        {
            var revisions = db.SourceRevisions.Where(r => r.Source.Provider == SourceProvider.Ntrs && r.Source.ExternalRecordId == id);
            var stored = await revisions.OrderByDescending(r => r.Id == r.Source.CurrentAcceptedRevisionId)
                .ThenByDescending(r => r.RevisionNumber).FirstOrDefaultAsync();
            if (stored != null) return stored;
            var connector = await db.NasaConnectors.SingleAsync(c => c.Key == "ntrs" && c.IsEnabled);
            var candidate = await nasa.FetchAsync(connector, id, default, requireMarsKeyword: false);
            return (await sources.StoreAsync(candidate, null, actorId, default)).Revision;
        }
        return (await sources.ManualAsync(new(source.Url!, source.Title!, [new(source.Author!, source.Organization!)],
            source.Organization!, SourceType.WebPage, entry.Claim, entry.Locator, DateTimeOffset.UtcNow, source.Author), default)).Revision;
    }
}
