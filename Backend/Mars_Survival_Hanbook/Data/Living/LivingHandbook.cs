using System.Security.Claims;
using System.Text.Json;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Living;

internal static class LivingHandbook
{
    private sealed record Guide(string Category, string Slug, string Title, string Summary, string Heading,
        string Body, string Discussion, string Application, string SourceTitle, string Url, string Locator,
        TestEnvironment Environment, string Claim);

    public static async Task PublishAsync(IServiceProvider services, CancellationToken ct)
    {
        await using var stream = typeof(LivingHandbook).Assembly.GetManifestResourceStream("LivingHandbook")
            ?? throw new InvalidOperationException("Missing Living on Mars handbook content.");
        var guides = await JsonSerializer.DeserializeAsync<Guide[]>(stream, DiscoveryJson.Options, ct)
            ?? throw new InvalidOperationException("Empty Living on Mars handbook content.");
        using var lockScope = services.CreateScope();
        var lockDb = lockScope.ServiceProvider.GetRequiredService<MarsDbContext>();
        await using var releaseLock = await lockDb.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(lockDb, "living-release-command", ct, timeoutMilliseconds: -1);

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<MarsDbContext>();
        if (!await db.Modules.AnyAsync(m => m.Slug == ContentModule.Living, ct))
            await DiscoverySeed.RunAsync(db, ct);
        var identity = await LivingEdition.ReleaseIdentityAsync(db, ct);
        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, identity.Id.ToString()), new Claim(ClaimTypes.Role, "Admin")
        ], "LocalContentRelease")) };
        try
        {
            var module = new ContentModule(ContentModule.Living);
            var articles = ActivatorUtilities.CreateInstance<ArticleService>(sp, module);
            var reviews = ActivatorUtilities.CreateInstance<ReviewService>(sp, module);
            var sources = sp.GetRequiredService<SourceService>();
            foreach (var guide in guides)
            {
                var article = await db.Articles.SingleOrDefaultAsync(a => a.Slug == guide.Slug, ct);
                if (article != null && article.CreatedById != identity.Id)
                    throw ApiException.Conflict($"Article slug already used: {guide.Slug}");
                if (article?.PublishedAt != null) continue;
                var (revision, _) = await sources.ManualAsync(new(guide.Url, guide.SourceTitle, [new("NASA", "NASA")],
                    "NASA", SourceType.WebPage, guide.Claim, guide.Locator, new DateTimeOffset(2026, 11, 16, 0, 0, 0, TimeSpan.Zero)), ct);
                if (revision.VerificationStatus == VerificationStatus.Pending && revision.RevokedAt == null)
                    await sources.DecideAsync(revision.Id, new(VerificationStatus.Accepted, guide.Claim), Concurrency.ETag(revision), ct);
                var evidence = guide.Environment switch
                {
                    TestEnvironment.Mars => EvidenceType.Observed,
                    TestEnvironment.DeepSpace => EvidenceType.Proposed,
                    _ => EvidenceType.Tested
                };
                var request = new ArticleDraftRequest { Slug = guide.Slug, CategorySlug = guide.Category,
                    LanguageCode = "en", Title = guide.Title, Summary = guide.Summary,
                    Sections = [new("evidence", guide.Heading, guide.Body), new("application", guide.Discussion, guide.Application)],
                    EnglishCitations = new() { [revision.Id] = new(guide.SourceTitle, null, guide.Locator) },
                    Claims = [new("finding", guide.Claim, evidence, guide.Environment, [revision.Id], new() { ["section"] = guide.Locator })] };
                article ??= await articles.SaveDraftAsync(null, request, null, ct);
                var version = await articles.VersionAsync(article.Id, article.CurrentDraftVersionId!.Value, ct);
                var snapshot = SnapshotService.Read(version);
                if (version.ChangedById != identity.Id || snapshot.ModuleSlug != module.Slug ||
                    snapshot.CategorySlug != guide.Category || snapshot.Title != guide.Title || snapshot.Summary != guide.Summary ||
                    !snapshot.Sections.SequenceEqual(request.Sections) || DiscoveryJson.Write(snapshot.Claims) != DiscoveryJson.Write(request.Claims) ||
                    snapshot.Sources.Single().SourceRevisionId != revision.Id)
                    throw ApiException.Gate($"The stored draft differs from the handbook edition: {guide.Slug}");
                await LivingEdition.ReviewAndPublishAsync(db, identity, article, version, articles, reviews, ct);
            }
            await releaseLock.CommitAsync(ct);
        }
        finally { accessor.HttpContext = previous; }
    }
}
