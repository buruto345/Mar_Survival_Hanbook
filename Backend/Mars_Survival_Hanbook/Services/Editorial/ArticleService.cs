using System.Data;
using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services.Editorial;

public sealed class ArticleService(MarsDbContext db, CurrentActor actor, SnapshotService snapshots, ContentModule module)
{
    public async Task<Article> GetAsync(Guid id, CancellationToken ct)
    {
        var a = await db.Articles.Include(x => x.Category).ThenInclude(x => x.Module).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.Missing();
        if (a.Category.Module.Slug != module.Slug || (module.Slug == ContentModule.Living && a.LanguageCode != "en")) throw ApiException.Missing();
        actor.ViewArticle(a); return a;
    }
    public async Task<ArticleVersion> VersionAsync(Guid articleId, Guid versionId, CancellationToken ct)
    {
        _ = await GetAsync(articleId, ct);
        return await db.ArticleVersions.SingleOrDefaultAsync(x => x.Id == versionId && x.ArticleId == articleId && (module.Slug != ContentModule.Living || x.LanguageCode == "en"), ct) ?? throw ApiException.Missing();
    }
    public async Task<Article> SaveDraftAsync(Guid? id, ArticleDraftRequest r, string? etag, CancellationToken ct)
    {
        if (module.Slug == ContentModule.Human && !r.HasExplicitLanguage) r.LanguageCode = "en";
        if (module.Slug == ContentModule.Survival && r.LanguageCode != "en")
            throw ApiException.Invalid("Survival Knowledge drafts require languageCode en.");
        if (module.Slug is ContentModule.Living or ContentModule.Human && r.LanguageCode != "en") throw ApiException.Invalid("languageCode must be en.");
        if (module.Slug == ContentModule.Science)
        {
            if (!r.HasExplicitLanguage) r.LanguageCode = "en";
            if (r.LanguageCode != "en" || !DiscoverySeed.ScienceCategories.Any(c => c.Slug == r.CategorySlug) || r.TechnologyProfile == null)
                throw ApiException.Invalid("Science profiles require English, one catalog category and technologyProfile.");
        }
        else if (r.TechnologyProfile != null) throw ApiException.Invalid("technologyProfile requires Science and Technology.");
        Validator.ValidateObject(r, new ValidationContext(r), true);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await DatabaseLocks.AcquireAsync(db, "article:" + (id?.ToString() ?? r.Slug), ct);
        if (r.TechnologyProfile is { } profile)
        {
            await DatabaseLocks.AcquireAsync(db, "official-profile:" + profile.TechnologyId, ct);
            if (!await db.Technologies.AnyAsync(t => t.Id == profile.TechnologyId && t.ArchivedAt == null, ct))
                throw ApiException.Invalid("Unknown or archived technology.");
            if (id.HasValue && !await db.ScienceTechnologyProfiles.AnyAsync(p => p.ArticleId == id && p.TechnologyId == profile.TechnologyId, ct))
            {
                _ = await GetAsync(id.Value, ct);
                throw ApiException.Conflict("The official technology cannot change.");
            }
            if (!id.HasValue && await db.ScienceTechnologyProfiles.AnyAsync(p => p.TechnologyId == profile.TechnologyId, ct))
                throw ApiException.Conflict("This technology already has an official profile.");
        }
        Article? a = null;
        if (id.HasValue)
        {
            a = await GetAsync(id.Value, ct); actor.Own(a.CreatedById); Concurrency.Check(a, etag);
            if (a.ArchivedAt != null || a.Status == ArticleStatus.Archived) throw ApiException.Conflict("The article is archived.");
            if (a.PublishedVersionId != null && a.Slug != r.Slug) throw ApiException.Conflict("The slug of a published article cannot change.");
        }
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Slug == r.CategorySlug && x.Module.Slug == module.Slug && x.ArchivedAt == null, ct)
            ?? throw ApiException.Invalid("The category does not belong to this module.");
        if (a == null)
        {
            a = new() { CategoryId = category.Id, CreatedById = actor.Id,
                Title = r.Title, Slug = r.Slug, Summary = r.Summary, Content = "", LanguageCode = r.LanguageCode };
            db.Articles.Add(a); await db.SaveChangesAsync(ct);
            if (r.TechnologyProfile != null) db.ScienceTechnologyProfiles.Add(new() { ArticleId = a.Id, TechnologyId = r.TechnologyProfile.TechnologyId });
        }
        a.CategoryId = category.Id; a.Title = r.Title; a.Slug = r.Slug; a.Summary = r.Summary;
        a.LanguageCode = r.LanguageCode; a.Content = string.Join("\n\n", r.Sections.Select(x => x.Heading + "\n" + x.Body)); a.UpdatedById = actor.Id;
        var s = await snapshots.BuildAsync(a.Id, r, module.Slug, ct);
        await UpdateWorkingLinksAsync(a.Id, s, ct);
        await AppendAsync(a, s, r.ChangeSummary, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return a;
    }
    private async Task AppendAsync(Article a, ArticleSnapshot s, string? summary, CancellationToken ct)
    {
        if (a.CurrentDraftVersionId.HasValue && a.CurrentDraftVersionId != a.PublishedVersionId)
        {
            var previous = await db.ArticleVersions.SingleAsync(x => x.Id == a.CurrentDraftVersionId, ct);
            if (previous.ReviewState is ArticleVersionReviewState.Draft or ArticleVersionReviewState.InReview)
                previous.ReviewState = ArticleVersionReviewState.Superseded;
            foreach (var task in await db.ReviewTasks.Where(x => x.ArticleVersionId == previous.Id &&
                (x.State == ReviewState.Pending || x.State == ReviewState.InProgress)).ToListAsync(ct))
            { task.State = ReviewState.Cancelled; task.CompletedAt = DateTimeOffset.UtcNow; }
        }
        var versionId = Guid.NewGuid();
        s.Media = s.Media.Select(m =>
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(m.GetRawText())!.AsObject();
            if (SnapshotService.Text(m, "fileChecksum") is { Length: 64 })
                node["url"] = $"/api/{s.ModuleSlug}/media/{m.GetProperty("id").GetGuid()}/file?articleVersionId={versionId}&language={s.LanguageCode}";
            return System.Text.Json.JsonSerializer.SerializeToElement(node, DiscoveryJson.Options);
        }).ToArray();
        var json = DiscoveryJson.Write(s);
        var version = new ArticleVersion { Id = versionId, ArticleId = a.Id, ChangedById = actor.Id,
            Version = (await db.ArticleVersions.Where(x => x.ArticleId == a.Id).MaxAsync(x => (int?)x.Version, ct) ?? 0) + 1,
            Snapshot = json, SnapshotHash = SourceNormalization.Hash(json), SnapshotSchemaVersion = s.SchemaVersion,
            Title = s.Title, Summary = s.Summary, CategorySlug = s.CategorySlug, LanguageCode = s.LanguageCode,
            SearchText = string.Join(" ", s.Sections.Select(x => x.Heading + " " + x.Body).Concat(s.Claims.Select(x => x.Text))),
            ChangeSummary = summary };
        db.ArticleVersions.Add(version);
        foreach (var citation in s.Sources)
            db.ArticleVersionSources.Add(new() { ArticleVersionId = version.Id, SourceId = citation.SourceId, SourceRevisionId = citation.SourceRevisionId });
        snapshots.AddProjection(version, s);
        // Break the Article <-> ArticleVersion insertion cycle before setting the pointer.
        await db.SaveChangesAsync(ct);
        a.CurrentDraftVersionId = version.Id;
        if (a.Status != ArticleStatus.Published) a.Status = ArticleStatus.Draft;
        Audit.Add(db, actor.Id, nameof(ArticleVersion), version.Id, AuditAction.Created, summary ?? "Saved immutable draft", actor.TraceId);
    }
    private async Task UpdateWorkingLinksAsync(Guid id, ArticleSnapshot s, CancellationToken ct)
    {
        db.ArticleSections.RemoveRange(await db.ArticleSections.Where(x => x.ArticleId == id).ToListAsync(ct));
        db.ArticleSources.RemoveRange(await db.ArticleSources.Where(x => x.ArticleId == id).ToListAsync(ct));
        db.ArticleFacts.RemoveRange(await db.ArticleFacts.Where(x => x.ArticleId == id).ToListAsync(ct));
        db.ArticleMedia.RemoveRange(await db.ArticleMedia.Where(x => x.ArticleId == id).ToListAsync(ct));
        await RemoveLinksAsync<ArticleMission>(id, ct);
        await RemoveLinksAsync<ArticleLocation>(id, ct);
        await RemoveLinksAsync<ArticleExplorationEvent>(id, ct);
        await RemoveLinksAsync<ArticleMeasurement>(id, ct);
        await RemoveLinksAsync<ArticleMapLayer>(id, ct);
        await db.SaveChangesAsync(ct);
        for (var i = 0; i < s.Sections.Length; i++)
            db.ArticleSections.Add(new() { ArticleId = id, Anchor = s.Sections[i].Anchor,
                Heading = s.Sections[i].Heading, Body = s.Sections[i].Body, DisplayOrder = i });
        foreach (var group in s.Sources.GroupBy(x => x.SourceId))
        {
            var revisionIds = group.Select(x => x.SourceRevisionId).ToArray();
            var claims = s.Claims.Where(x => x.SourceRevisionIds.Intersect(revisionIds).Any()).ToArray();
            db.ArticleSources.Add(new() { ArticleId = id, SourceId = group.Key, SourceRevisionId = group.First().SourceRevisionId,
                ClaimNote = "Claim-level evidence and locators are preserved in the immutable article snapshot.",
                EvidenceType = claims.Select(x => x.EvidenceType).Distinct().Count() == 1 ? claims[0].EvidenceType : EvidenceType.Unspecified,
                TestEnvironment = claims.Select(x => x.TestEnvironment).Distinct().Count() == 1 ? claims[0].TestEnvironment : TestEnvironment.Unspecified });
        }
        foreach (var (value, order) in s.Facts.Select((v, i) => (v, i)))
            db.ArticleFacts.Add(new() { ArticleId = id, FactId = value.GetProperty("id").GetGuid(), DisplayOrder = order });
        foreach (var (value, order) in s.Media.Select((v, i) => (v, i)))
            db.ArticleMedia.Add(new() { ArticleId = id, MediaId = value.GetProperty("id").GetGuid(),
                DisplayOrder = value.TryGetProperty("displayOrder", out var display) ? display.GetInt32() : order,
                Caption = SnapshotService.Text(value, "caption") });
        foreach (var value in s.Missions) db.Add(new ArticleMission { ArticleId = id, MissionId = value.GetProperty("id").GetGuid() });
        foreach (var value in s.Locations) db.Add(new ArticleLocation { ArticleId = id, LocationId = value.GetProperty("id").GetGuid() });
        foreach (var value in s.TimelineEvents) db.Add(new ArticleExplorationEvent { ArticleId = id, ExplorationEventId = value.GetProperty("id").GetGuid() });
        foreach (var value in s.Measurements) db.Add(new ArticleMeasurement { ArticleId = id, MeasurementId = value.GetProperty("id").GetGuid() });
        foreach (var value in s.MapLayers) db.Add(new ArticleMapLayer { ArticleId = id, MapLayerId = value.GetProperty("id").GetGuid() });
    }
    private async Task RemoveLinksAsync<T>(Guid id, CancellationToken ct) where T : class =>
        db.RemoveRange(await db.Set<T>().Where(x => EF.Property<Guid>(x, "ArticleId") == id).ToListAsync(ct));
    public async Task<ReviewTask> SubmitAsync(Guid id, Guid versionId, string? etag, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "publication", ct);
        var a = await GetAsync(id, ct); actor.Own(a.CreatedById); Concurrency.Check(a, etag);
        var v = await VersionAsync(id, versionId, ct);
        if (a.CurrentDraftVersionId != v.Id || v.ReviewState != ArticleVersionReviewState.Draft)
            throw ApiException.Conflict("Only the current draft can be submitted, and only once.");
        await snapshots.ValidateGateAsync(v, ct);
        var task = new ReviewTask { ArticleId = id, ArticleVersionId = v.Id, SubmittedById = actor.Id };
        db.ReviewTasks.Add(task); v.ReviewState = ArticleVersionReviewState.InReview;
        if (a.Status != ArticleStatus.Published) a.Status = ArticleStatus.InReview;
        a.UpdatedById = actor.Id; a.UpdatedAt = DateTimeOffset.UtcNow;
        Audit.Add(db, actor.Id, nameof(ArticleVersion), v.Id, AuditAction.SubmittedForReview, "Submitted frozen snapshot", actor.TraceId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return task;
    }
    public async Task<Article> PublishAsync(Guid id, Guid versionId, string? etag, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "publication", ct);
        var a = await GetAsync(id, ct); Concurrency.Check(a, etag);
        var v = await VersionAsync(id, versionId, ct);
        if (a.ArchivedAt != null || a.Status == ArticleStatus.Archived || a.CurrentDraftVersionId != v.Id ||
            v.ReviewState != ArticleVersionReviewState.Approved || v.ReviewedAt == null || v.ReviewedById == null)
            throw ApiException.Gate("Only the approved current draft can be published.");
        if (!await db.ReviewTasks.AnyAsync(x => x.ArticleVersionId == v.Id && x.State == ReviewState.Approved &&
            x.SourcesChecked && x.ClaimsChecked && x.UnitsChecked && x.EvidenceChecked && x.MediaRightsChecked &&
            x.ReviewerId == v.ReviewedById && x.CompletedAt != null, ct)) throw ApiException.Gate("Approval is missing for this version.");
        await snapshots.ValidateGateAsync(v, ct);
        if (a.PublishedVersionId == v.Id && a.Status == ArticleStatus.Published) { await tx.CommitAsync(ct); return a; }
        a.PublishedVersionId = v.Id; a.Status = ArticleStatus.Published;
        a.ReviewedAt = v.ReviewedAt; a.ReviewedById = v.ReviewedById;
        a.PublishedAt = DateTimeOffset.UtcNow; a.PublishedById = actor.Id; a.NextReviewAt = v.ReviewedAt.Value.AddMonths(6);
        a.RequiresReview = await db.ArticleVersionSources.Where(x => x.ArticleVersionId == v.Id).AnyAsync(l =>
            db.SourceRevisions.Any(r => r.SourceId == l.SourceId && r.RevisionNumber > l.SourceRevision.RevisionNumber &&
                r.VerificationStatus != VerificationStatus.Rejected), ct);
        Audit.Add(db, actor.Id, nameof(Article), id, AuditAction.Published, "Published version " + v.Id, actor.TraceId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return a;
    }
    public async Task<Article> UnpublishAsync(Guid id, string reason, string? etag, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "publication", ct);
        var a = await GetAsync(id, ct); Concurrency.Check(a, etag);
        a.Status = ArticleStatus.Draft;
        Audit.Add(db, actor.Id, nameof(Article), id, AuditAction.Unpublished, reason, actor.TraceId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return a;
    }
    public async Task<Article> RestoreAsync(Guid id, RestoreRequest r, string? etag, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await DatabaseLocks.AcquireAsync(db, "article:" + id, ct);
        var a = await GetAsync(id, ct); actor.Own(a.CreatedById); Concurrency.Check(a, etag);
        var sourceVersion = await VersionAsync(id, r.SourceVersionId, ct);
        if (module.Slug == ContentModule.Living) await snapshots.ValidateGateAsync(sourceVersion, ct);
        var s = SnapshotService.Read(sourceVersion);
        if (module.Slug == ContentModule.Science && sourceVersion.SnapshotHash != SourceNormalization.Hash(sourceVersion.Snapshot))
            throw ApiException.Gate("The snapshot hash does not match.");
        if (module.Slug == ContentModule.Human && (s.LanguageCode != "en" || sourceVersion.LanguageCode != "en" || s.ModuleSlug != module.Slug))
            throw ApiException.Invalid("Only English Human Survival versions can be restored.");
        if (module.Slug == ContentModule.Survival && s.LanguageCode != "en")
            throw ApiException.Invalid("Only English versions can be restored as Survival Knowledge drafts.");
        if (module.Slug == ContentModule.Science && (s.LanguageCode != "en" || s.ModuleSlug != module.Slug ||
            !DiscoverySeed.ScienceCategories.Any(c => c.Slug == s.CategorySlug) ||
            !await db.Categories.AnyAsync(c => c.Slug == s.CategorySlug && c.Module.Slug == module.Slug && c.ArchivedAt == null, ct) || s.TechnologyProfile == null ||
            !await db.ScienceTechnologyProfiles.AnyAsync(p => p.ArticleId == id && p.TechnologyId == s.TechnologyProfile.TechnologyId, ct)))
            throw ApiException.Invalid("The version cannot be restored to this official profile.");
        a.Title = s.Title; a.Summary = s.Summary; a.LanguageCode = s.LanguageCode;
        a.Content = string.Join("\n\n", s.Sections.Select(x => x.Heading + "\n" + x.Body));
        a.CategoryId = await db.Categories.Where(x => x.Slug == s.CategorySlug && x.Module.Slug == module.Slug).Select(x => x.Id).SingleAsync(ct);
        await UpdateWorkingLinksAsync(a.Id, s, ct);
        await AppendAsync(a, s, r.ChangeSummary, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return a;
    }
}
