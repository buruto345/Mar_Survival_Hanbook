using System.Security.Claims;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Integrations.Nasa;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Survival;

// Explicit local operator command for the reviewed English first edition.
internal sealed class SurvivalRelease(MarsDbContext db, SourceService sources, ContentService content,
    ArticleService articles, ReviewService reviews, NasaConnectorClient nasa, CurrentActor actor)
{
    private sealed record Source(string Key, string Title, string Url, string Author, string Organization);
    private sealed record Fact(string Name, decimal Value, string Unit, MeasurementKind Kind,
        decimal? OriginalValue, string? OriginalUnit, decimal? UpperValue);
    private sealed record Section(string Anchor, string Heading, string Text, string Source, string Locator,
        Fact? Fact, EvidenceType Evidence = EvidenceType.Observed, TestEnvironment Environment = TestEnvironment.Mars);
    private sealed record Entry(string Category, string Slug, string Title, string Summary, Section[] Sections);
    private sealed record Pack(Source[] Sources, Entry[] Articles);

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<MarsDbContext>();
        await DiscoverySeed.RunAsync(db);
        const string username = "survival-release";
        await using var tx = await db.Database.BeginTransactionAsync();
        await DatabaseLocks.AcquireAsync(db, username, default);
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedUsername == "SURVIVAL-RELEASE");
        if (user == null)
        {
            user = new User { Username = username, NormalizedUsername = "SURVIVAL-RELEASE",
                FullName = "Survival Knowledge local content release", Role = UserRole.Admin,
                State = UserState.Deactivated, PasswordHash = "!disabled-survival-release" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        if (user.State != UserState.Deactivated || user.Role != UserRole.Admin || user.PasswordHash != "!disabled-survival-release")
            throw new InvalidOperationException("Reserved local release identity has changed.");
        await tx.CommitAsync();

        var accessor = sp.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, nameof(UserRole.Admin))
        ], "LocalContentRelease")) };
        try
        {
            var module = new ContentModule(ContentModule.Survival);
            var release = ActivatorUtilities.CreateInstance<SurvivalRelease>(sp,
                ActivatorUtilities.CreateInstance<ArticleService>(sp, module),
                ActivatorUtilities.CreateInstance<ReviewService>(sp, module));
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "Survival", "release.en.json");
            await release.ImportAsync(DiscoveryJson.Read<Pack>(await File.ReadAllTextAsync(path)));
        }
        finally { accessor.HttpContext = previous; }
    }

    private async Task ImportAsync(Pack pack)
    {
        var revisions = new Dictionary<string, Guid>();
        foreach (var source in pack.Sources)
        {
            var sections = pack.Articles.SelectMany(a => a.Sections).Where(s => s.Source == source.Key);
            var note = string.Join("\n", sections.Select(s => s.Locator + " — " + s.Text));
            revisions[source.Key] = await RevisionAsync(source, note);
        }
        foreach (var entry in pack.Articles)
        {
            var existing = await db.Articles.SingleOrDefaultAsync(a => a.Slug == entry.Slug);
            if (existing != null && existing.CreatedById != actor.Id)
                throw new InvalidOperationException("Article slug already in use: " + entry.Slug);
            // Leave later edits, unpublishing and revocation decisions with the CMS.
            var refreshCitation = false;
            if (existing?.PublishedVersionId is Guid publishedId)
            {
                if (existing.Status != ArticleStatus.Published) continue;
                var published = await articles.VersionAsync(existing.Id, publishedId, default);
                var snapshot = SnapshotService.Read(published);
                // Replace citations from an earlier release run that still pinned translated source notes.
                if (published.ChangedById != actor.Id ||
                    !snapshot.Sources.Any(c => c.UsageNote == PreviousUsageNote)) continue;
                refreshCitation = existing.CurrentDraftVersionId == publishedId;
            }
            var facts = new List<Guid>();
            foreach (var s in entry.Sections.Where(s => s.Fact != null))
                facts.Add(await FactAsync(s, revisions[s.Source]));
            var draft = new ArticleDraftRequest {
                CategorySlug = entry.Category, Slug = entry.Slug, LanguageCode = "en", Title = entry.Title, Summary = entry.Summary,
                Sections = entry.Sections.Select(s => new SectionInput(s.Anchor, s.Heading, s.Text)).ToArray(),
                Claims = entry.Sections.Select(s => new ClaimInput(s.Anchor, s.Text, s.Evidence, s.Environment,
                    [revisions[s.Source]], new() { ["section"] = s.Locator })).ToArray(),
                ResourceRefs = new() { FactIds = facts.ToArray() }, ChangeSummary = "Survival Knowledge EN, first edition"
            };
            var article = existing == null || refreshCitation
                ? await articles.SaveDraftAsync(existing?.Id, draft, existing == null ? null : Concurrency.ETag(existing), default)
                : existing;
            var version = await articles.VersionAsync(article.Id, article.CurrentDraftVersionId!.Value, default);
            if (version.ChangedById != actor.Id || version.ChangeSummary != draft.ChangeSummary)
                throw new InvalidOperationException("Article has entered a different editorial workflow: " + entry.Slug);
            var task = version.ReviewState == ArticleVersionReviewState.Draft
                ? await articles.SubmitAsync(article.Id, version.Id, Concurrency.ETag(article), default)
                : await db.ReviewTasks.SingleAsync(t => t.ArticleVersionId == version.Id);
            if (task.State == ReviewState.Pending) await reviews.ClaimAsync(task.Id, Concurrency.ETag(task), default);
            if (task.State == ReviewState.InProgress && task.ReviewerId == actor.Id)
                await reviews.DecideAsync(task.Id, new(ReviewState.Approved, draft.ChangeSummary,
                    new(true, true, true, true, true)), Concurrency.ETag(task), default);
            await articles.PublishAsync(article.Id, version.Id, Concurrency.ETag(article), default);
            Console.WriteLine($"{article.Slug} v{version.Version} {version.Id}");
        }
        // Withdraw only the earlier, release-owned edition, after all replacements are available.
        var previousEdition = await db.Articles.Where(a => a.CreatedById == actor.Id &&
            a.Category.Module.Slug == ContentModule.Survival && a.Status == ArticleStatus.Published &&
            db.ArticleVersions.Any(v => v.Id == a.PublishedVersionId && v.LanguageCode == "vi" &&
                v.ChangeSummary == "Survival Knowledge VI, first edition")).ToListAsync();
        foreach (var article in previousEdition)
            await articles.UnpublishAsync(article.Id, "Replaced by the English edition.", Concurrency.ETag(article), default);
    }

    private const string PreviousUsageNote = "Vietnamese paraphrase with section locators; no image reproduced.";

    private async Task<Guid> RevisionAsync(Source source, string note)
    {
        var existing = await db.ScientificSources.SingleOrDefaultAsync(x => x.CanonicalUrl == source.Url);
        if (existing?.CurrentAcceptedRevisionId is Guid accepted)
        {
            var record = await db.SourceRevisions.SingleAsync(r => r.Id == accepted);
            if (DiscoveryJson.Read<SourceMetadata>(record.MetadataSnapshot).UsageNote != PreviousUsageNote)
                return accepted;
        }
        if (existing != null && await db.SourceRevisions.AnyAsync(r => r.SourceId == existing.Id && r.RevokedAt != null))
            throw new InvalidOperationException("Source has a revocation history; review in the CMS.");
        SourceRevision revision;
        if (source.Key == "ames")
        {
            var connector = await db.NasaConnectors.SingleAsync(x => x.Key == "ntrs" && x.IsEnabled);
            var candidate = await nasa.FetchAsync(connector, "20210019591", default);
            if (candidate.Metadata.Title != source.Title || candidate.Metadata.Excerpt?.Contains("below 0.4 g") != true)
                throw new InvalidOperationException("NTRS record has changed. Review the new text.");
            (revision, _) = await sources.StoreAsync(candidate, null, actor.Id, default);
        }
        else
        {
            (revision, _) = await sources.ManualAsync(new(source.Url, source.Title, [new(source.Author, source.Organization)],
                source.Organization, SourceType.WebPage, note, "English paraphrase with section locators; no image reproduced.",
                new DateTimeOffset(2026, 11, 7, 0, 0, 0, TimeSpan.Zero), source.Organization), default);
        }
        if (revision.VerificationStatus != VerificationStatus.Accepted)
            await sources.DecideAsync(revision.Id, new(VerificationStatus.Accepted, note), Concurrency.ETag(revision), default);
        return revision.Id;
    }

    private async Task<Guid> FactAsync(Section section, Guid revisionId)
    {
        var f = section.Fact!;
        await using var tx = await db.Database.BeginTransactionAsync();
        await DatabaseLocks.AcquireAsync(db, "survival-fact:" + f.Name, default);
        var unit = await db.Units.SingleAsync(u => u.Symbol == f.Unit && u.ArchivedAt == null);
        // Accepted facts are immutable. Reuse the English context, or translate a pending candidate.
        var candidates = db.ScientificFacts.Where(x => x.Name == f.Name && x.SourceRevisionId == revisionId);
        var fact = await candidates.SingleOrDefaultAsync(x => x.MeasurementContext == section.Text)
            ?? await candidates.SingleOrDefaultAsync(x => x.VerificationStatus == VerificationStatus.Pending);
        var request = new FactRequest { Name = f.Name, Value = f.Value, UpperValue = f.UpperValue,
            UnitId = unit.Id, MeasurementKind = f.Kind, OriginalValue = f.OriginalValue ?? f.Value,
            OriginalUnit = f.OriginalUnit ?? f.Unit, Context = section.Text, EvidenceType = section.Evidence,
            TestEnvironment = section.Environment, SourceRevisionId = revisionId };
        if (fact != null && (fact.ArchivedAt != null || fact.VerificationStatus == VerificationStatus.Rejected ||
            fact.Value != request.Value || fact.UpperValue != request.UpperValue || fact.OriginalValue != request.OriginalValue ||
            fact.OriginalUnit != request.OriginalUnit || fact.UnitId != unit.Id || fact.MeasurementKind != f.Kind))
            throw new InvalidOperationException("Existing fact differs from reviewed value: " + f.Name);
        if (fact?.VerificationStatus != VerificationStatus.Accepted)
        {
            var saved = await content.SaveAsync("facts", fact?.Id, request, fact == null ? null : Concurrency.ETag(fact), default);
            fact = (ScientificFact)saved.Entity;
            await content.ReviewFactAsync(fact.Id, new(VerificationStatus.Accepted, section.Locator), Concurrency.ETag(fact), default);
        }
        await tx.CommitAsync();
        return fact.Id;
    }
}
