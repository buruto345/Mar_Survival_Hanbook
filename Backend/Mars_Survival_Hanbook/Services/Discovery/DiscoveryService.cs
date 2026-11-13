using System.Text.Json;
using System.Text.Json.Nodes;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services.Editorial;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services.Discovery;

public sealed class Publication
{
    public Article Article { get; set; } = null!;
    public ArticleVersion Version { get; set; } = null!;
}
public sealed class PublicResourceRow
{
    public PublicationResource Resource { get; set; } = null!;
    public Guid ArticleId { get; set; }
    public Guid ArticleVersionId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string CategorySlug { get; set; } = "";
}
public sealed class DiscoveryService(MarsDbContext db, IConfiguration config, ContentModule module)
{
    private static readonly string[] ScienceSlugs = DiscoverySeed.ScienceCategories.Select(c => c.Slug).ToArray();

    public IQueryable<Publication> Publications(string? language) =>
        from a in db.Articles.AsNoTracking()
        join v in db.ArticleVersions.AsNoTracking() on a.PublishedVersionId equals v.Id
        where a.Status == ArticleStatus.Published && a.ArchivedAt == null && a.Category.Module.Slug == module.Slug &&
            (module.Slug != ContentModule.Science || (v.LanguageCode == "en" && v.ArticleId == a.Id &&
                ScienceSlugs.Contains(v.CategorySlug) && db.ScienceTechnologyProfiles.Any(p => p.ArticleId == a.Id &&
                    p.TechnologyId.ToString().ToLower() == MarsDbContext.JsonValue(v.Snapshot, "$.technologyProfile.technologyId")) &&
                db.ArticleVersionSources.Any(l => l.ArticleVersionId == v.Id) &&
                MarsDbContext.JsonValue(v.Snapshot, "$.moduleSlug") == ContentModule.Science &&
                MarsDbContext.JsonValue(v.Snapshot, "$.languageCode") == "en")) &&
            ((module.Slug != ContentModule.Human && module.Slug != ContentModule.Survival) || v.LanguageCode == "en") &&
            (module.Slug != ContentModule.Living || (v.LanguageCode == "en" &&
                db.LivingEnglishReviews.Any(review => review.ArticleVersionId == v.Id && review.SnapshotHash == v.SnapshotHash))) &&
            (language == null || v.LanguageCode == language) && v.SnapshotSchemaVersion == 1 &&
            !db.ArticleVersionSources.Any(l => l.ArticleVersionId == v.Id &&
                (l.SourceRevision.RevokedAt != null || l.SourceRevision.VerificationStatus != VerificationStatus.Accepted))
        select new Publication { Article = a, Version = v };

    public async Task<object> ModuleAsync(string language, CancellationToken ct)
    {
        var m = await db.Modules.AsNoTracking().Where(x => x.Slug == module.Slug && x.ArchivedAt == null)
            .Select(x => new { x.Id, x.Name, x.Slug, x.Description }).SingleOrDefaultAsync(ct) ?? throw ApiException.Missing();
        var cover = await (from p in Publications(language)
            join media in db.PublicationResources on p.Version.Id equals media.ArticleVersionId
            where media.Kind == "media" && MarsDbContext.JsonValue(media.Payload, "$.mediaType") == "Image"
            orderby p.Article.PublishedAt descending, p.Article.Id, media.ResourceId
            select MarsDbContext.JsonValue(media.Payload, "$.url")).FirstOrDefaultAsync(ct);
        return new { m.Id, m.Name, m.Slug, m.Description, Categories = await CategoriesAsync(language, ct),
            PublishedArticleCount = await Publications(language).CountAsync(ct), CoverUrl = cover };
    }
    public async Task<object[]> CategoriesAsync(string language, CancellationToken ct)
    {
        var counts = await Publications(language).GroupBy(x => x.Version.CategorySlug)
            .Select(x => new { Slug = x.Key, Count = x.Count(), SlugIfSingle = x.Count() == 1 ? x.Min(p => p.Article.Slug) : null }).ToDictionaryAsync(x => x.Slug, ct);
        var categories = await db.Categories.AsNoTracking().Where(x => x.Module.Slug == module.Slug && x.ArchivedAt == null &&
            (module.Slug != ContentModule.Science || ScienceSlugs.Contains(x.Slug)))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        if (module.Slug == ContentModule.Discovery)
            return categories.Select(x => DiscoverCategoryImage.Category(x.Id, x.Slug, x.Name, x.Description,
                counts.GetValueOrDefault(x.Slug)?.Count ?? 0)).ToArray();
        if (module.Slug == ContentModule.Human)
            return categories.Select(x => (object)new HumanCategoryResponse(x.Id, x.Slug, x.Name, x.Description,
                counts.GetValueOrDefault(x.Slug)?.Count ?? 0, counts.GetValueOrDefault(x.Slug)?.SlugIfSingle)).ToArray();
        return categories.Select(x => (object)new { x.Id, x.Slug, x.Name, x.Description, PublishedArticleCount = counts.GetValueOrDefault(x.Slug)?.Count ?? 0 }).ToArray();
    }
    public async Task<object> CategoryAsync(string slug, string language, CancellationToken ct)
    {
        var c = await db.Categories.AsNoTracking().Where(x => x.Module.Slug == module.Slug && x.Slug == slug && x.ArchivedAt == null)
            .Select(x => new { x.Id, x.Slug, x.Name, x.Description }).SingleOrDefaultAsync(ct) ?? throw ApiException.Missing();
        var count = await Publications(language).CountAsync(x => x.Version.CategorySlug == slug, ct);
        if (module.Slug == ContentModule.Discovery)
            return DiscoverCategoryImage.Category(c.Id, c.Slug, c.Name, c.Description, count);
        return new { c.Id, c.Slug, c.Name, c.Description, PublishedArticleCount = count };
    }
    public async Task<object> ArticlesAsync(DiscoveryQuery q, CancellationToken ct)
    {
        var query = Publications(q.Language);
        if (q.Category != null) query = query.Where(x => x.Version.CategorySlug == q.Category);
        if (!string.IsNullOrEmpty(q.Search)) query = query.Where(x => x.Version.Title.Contains(q.Search) ||
            x.Version.Summary.Contains(q.Search) || x.Version.SearchText.Contains(q.Search));
        var ordered = q.Sort == "relevance" && !string.IsNullOrEmpty(q.Search)
            ? query.OrderByDescending(x => x.Version.Title.Contains(q.Search)).ThenByDescending(x => x.Article.PublishedAt).ThenBy(x => x.Article.Id)
            : query.OrderByDescending(x => x.Article.PublishedAt).ThenBy(x => x.Article.Id);
        return await ordered.Select(x => new {
            x.Article.Id, x.Article.Slug, x.Version.Title, x.Version.Summary, x.Version.CategorySlug, x.Version.LanguageCode,
            ArticleVersionId = x.Version.Id, x.Version.Version, x.Version.ReviewedAt, x.Article.PublishedAt,
            ThumbnailUrl = db.PublicationResources.Where(p => p.ArticleVersionId == x.Version.Id && p.Kind == "media" &&
                MarsDbContext.JsonValue(p.Payload, "$.mediaType") == "Image").OrderBy(p => p.ResourceId)
                .Select(p => MarsDbContext.JsonValue(p.Payload, "$.url")).FirstOrDefault()
        }).PageAsync(q, ct);
    }
    public Task<PageResult<TechnologyCard>> TechnologyCardsAsync(PageQuery query, string? category, CancellationToken ct)
    {
        if (category != null && !DiscoverySeed.ScienceCategories.Any(c => c.Slug == category))
            throw ApiException.Invalid("Unknown science category.");
        return Publications("en").Where(p => category == null || p.Version.CategorySlug == category)
            .OrderByDescending(p => p.Article.PublishedAt).ThenBy(p => p.Article.Id)
            .Select(p => new TechnologyCard(p.Article.Id, p.Article.Slug, p.Version.Title, p.Version.Summary, p.Version.CategorySlug,
                MarsDbContext.JsonValue(p.Version.Snapshot, "$.technologyProfile.technologyStatus")!,
                MarsDbContext.JsonValue(p.Version.Snapshot, "$.technologyProfile.testEnvironment")!)).PageAsync(query, ct);
    }
    public async Task<object> ArticleAsync(string slug, string language, CancellationToken ct)
    {
        var p = await Publications(language).SingleOrDefaultAsync(x => x.Article.Slug == slug, ct) ?? throw ApiException.Missing();
        return await RenderAsync(p.Article, p.Version, true, ct);
    }
    public async Task<object> RenderAsync(Article a, ArticleVersion v, bool publicOnly, CancellationToken ct)
    {
        var s = SnapshotService.Read(v);
        var node = JsonSerializer.SerializeToNode(s, DiscoveryJson.Options)!.AsObject();
        node["id"] = a.Id; node["slug"] = a.Slug; node["articleVersionId"] = v.Id; node["version"] = v.Version;
        node["reviewedAt"] = JsonSerializer.SerializeToNode(v.ReviewedAt, DiscoveryJson.Options);
        node["publishedAt"] = JsonSerializer.SerializeToNode(a.PublishedAt, DiscoveryJson.Options);
        node["tableOfContents"] = JsonSerializer.SerializeToNode(s.Sections.Select(x => new { x.Anchor, x.Heading }), DiscoveryJson.Options);
        foreach (var key in new[] { "facts", "measurements", "missions", "locations", "timelineEvents", "media", "mapLayers" })
            foreach (var block in node[key]!.AsArray()) block!["articleVersionId"] = v.Id;
        var related = await Publications(s.LanguageCode).Where(x => s.RelatedArticleIds.Contains(x.Article.Id))
            .Select(x => new { x.Article.Id, x.Article.Slug, x.Version.Title, ArticleVersionId = x.Version.Id }).ToListAsync(ct);
        node["relatedArticles"] = JsonSerializer.SerializeToNode(related, DiscoveryJson.Options);
        if (module.Slug == ContentModule.Living || module.Slug == ContentModule.Human && s.CategorySlug == "physical-health")
        {
            var relatedSlug = s.CategorySlug switch {
                "energy" => "dust-storms-and-solar-power", "water-and-oxygen" => "oxygen-and-surface-pressure",
                "housing" => "radiation-at-curiosity", "community" or "physical-health" => "partial-gravity-and-human-health", _ => null
            };
            var survival = new DiscoveryService(db, config, new ContentModule(ContentModule.Survival));
            if (relatedSlug != null && await survival.Publications("en").AnyAsync(x => x.Article.Slug == relatedSlug, ct))
                node["survivalKnowledgeUrl"] = "/api/survival-knowledge/articles/" + relatedSlug + "?language=en";
        }
        if ((module.Slug is ContentModule.Science or ContentModule.Human) && s.RelatedArticleIds.Length > 0)
        {
            var accessible = new JsonArray();
            foreach (var target in new[] { module.Slug, ContentModule.Science, ContentModule.Discovery, ContentModule.Living, ContentModule.Survival }.Distinct())
            {
                var targetService = new DiscoveryService(db, config, new ContentModule(target));
                var rows = await targetService.Publications(null).Where(p => s.RelatedArticleIds.Contains(p.Article.Id))
                    .Select(p => new { p.Article.Id, p.Article.Slug, p.Version.Title, ArticleVersionId = p.Version.Id, p.Version.LanguageCode }).ToListAsync(ct);
                foreach (var row in rows)
                {
                    var link = JsonSerializer.SerializeToNode(row, DiscoveryJson.Options)!.AsObject();
                    var route = target == ContentModule.Science ? "technologies" : "articles";
                    link["url"] = $"/api/{target}/{route}/{row.Slug}?language={row.LanguageCode}";
                    accessible.Add(link);
                }
            }
            node["relatedArticles"] = accessible;
        }
        node.Remove("relatedArticleIds");
        node["bodyFormat"] = "plainText";
        return node;
    }
    private IQueryable<PublicResourceRow> ResourceRows(string? language) =>
        from p in Publications(language)
        join r in db.PublicationResources.AsNoTracking() on p.Version.Id equals r.ArticleVersionId
        select new PublicResourceRow { Resource = r, ArticleId = p.Article.Id, ArticleVersionId = p.Version.Id,
            PublishedAt = p.Article.PublishedAt, CategorySlug = p.Version.CategorySlug };
    public async Task<object> ResourcesAsync(string kind, DiscoveryQuery q, CancellationToken ct)
    {
        var bbox = ContentValidation.Bbox(q.Bbox);
        if (q.From > q.To) throw ApiException.Invalid("The start time must precede the end time.");
        if (kind == "measurements" && ((!q.FactId.HasValue && !q.MissionId.HasValue) || !q.From.HasValue || !q.To.HasValue ||
            (q.To.Value - q.From.Value).TotalDays > config.GetValue("Discovery:MaxMeasurementDays", 31)))
            throw ApiException.Invalid("Supply a factId or missionId and a supported measurement date range.");
        var all = ResourceRows(q.Language).Where(x => x.Resource.Kind == kind);
        // Select the canonical representation before applying user filters.
        var query = all.Where(x => !all.Any(y => y.Resource.ResourceId == x.Resource.ResourceId &&
            (y.PublishedAt > x.PublishedAt || (y.PublishedAt == x.PublishedAt && y.ArticleId.CompareTo(x.ArticleId) < 0))));
        if (!string.IsNullOrEmpty(q.Search)) query = query.Where(x => x.Resource.Name.Contains(q.Search));
        if (q.Category != null) query = query.Where(x => x.CategorySlug == q.Category);
        if (q.Status.HasValue) { var status = q.Status.ToString(); query = query.Where(x => x.Resource.Status == status); }
        if (q.Type.HasValue) { var type = q.Type.ToString(); query = query.Where(x => x.Resource.Type == type); }
        if (q.QuantityKind != null) query = query.Where(x => x.Resource.QuantityKind == q.QuantityKind);
        if (q.EvidenceType.HasValue) { var evidence = q.EvidenceType.ToString(); query = query.Where(x => x.Resource.EvidenceType == evidence); }
        if (q.MissionId.HasValue) query = query.Where(x => x.Resource.MissionId == q.MissionId);
        if (q.LocationId.HasValue) query = query.Where(x => x.Resource.LocationId == q.LocationId);
        if (q.FactId.HasValue) query = query.Where(x => x.Resource.FactId == q.FactId);
        if (q.From.HasValue) query = query.Where(x => x.Resource.Date >= q.From);
        if (q.To.HasValue) query = query.Where(x => x.Resource.Date <= q.To);
        if (kind == "timeline-events" && !q.IncludePlanned) query = query.Where(x => !x.Resource.IsPlanned);
        if (bbox != null)
        {
            var w = bbox[0]; var south = bbox[1]; var e = bbox[2]; var n = bbox[3];
            query = query.Where(x => x.Resource.Longitude >= w && x.Resource.Longitude <= e &&
                x.Resource.Latitude >= south && x.Resource.Latitude <= n);
        }
        var result = await query.OrderBy(x => x.Resource.Date).ThenBy(x => x.Resource.ResourceId).PageAsync(q, ct);
        return new PageResult<JsonObject>(result.Items.Select(ResourceDto).ToArray(), q.Page, q.PageSize, result.Total);
    }
    private static JsonObject ResourceDto(PublicResourceRow row)
    {
        var node = JsonNode.Parse(row.Resource.Payload)!.AsObject();
        node["articleVersionId"] = row.ArticleVersionId; node["articleId"] = row.ArticleId; return node;
    }
    public async Task<object> ResourceAsync(string kind, Guid id, Guid? articleVersionId, string? language, CancellationToken ct)
    {
        var query = ResourceRows(language).Where(x => x.Resource.Kind == kind && x.Resource.ResourceId == id);
        if (articleVersionId.HasValue) query = query.Where(x => x.ArticleVersionId == articleVersionId);
        var row = await query.OrderByDescending(x => x.PublishedAt).ThenBy(x => x.ArticleId).FirstOrDefaultAsync(ct) ?? throw ApiException.Missing();
        var result = ResourceDto(row);
        var v = await db.ArticleVersions.AsNoTracking().SingleAsync(x => x.Id == row.ArticleVersionId, ct);
        var sourceId = result["sourceRevisionId"]?.GetValue<Guid>();
        result["source"] = JsonSerializer.SerializeToNode(SnapshotService.Read(v).Sources.SingleOrDefault(x => x.SourceRevisionId == sourceId), DiscoveryJson.Options);
        var relatedIds = ResourceRows(language).Where(x => x.Resource.Kind == kind && x.Resource.ResourceId == id).Select(x => x.ArticleId);
        result["relatedArticles"] = JsonSerializer.SerializeToNode(await Publications(language).Where(x => relatedIds.Contains(x.Article.Id))
            .OrderByDescending(x => x.Article.PublishedAt).ThenBy(x => x.Article.Id)
            .Select(x => new { x.Article.Id, x.Article.Slug, x.Version.Title, ArticleVersionId = x.Version.Id }).ToListAsync(ct), DiscoveryJson.Options);
        return result;
    }
    public async Task<object> SourceAsync(Guid id, Guid revisionId, CancellationToken ct)
    {
        var p = await Publications(null).Where(x => db.ArticleVersionSources.Any(l =>
            l.ArticleVersionId == x.Version.Id && l.SourceId == id && l.SourceRevisionId == revisionId))
            .OrderByDescending(x => x.Article.PublishedAt).ThenBy(x => x.Article.Id).FirstOrDefaultAsync(ct) ?? throw ApiException.Missing();
        return SnapshotService.Read(p.Version).Sources.Single(x => x.SourceId == id && x.SourceRevisionId == revisionId);
    }
}
