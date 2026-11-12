using System.Text.Json;
using System.Text.RegularExpressions;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services.Editorial;

public sealed class SnapshotService(MarsDbContext db, ContentService content, RawStorage raw)
{
    public async Task<ArticleSnapshot> BuildAsync(Guid articleId, ArticleDraftRequest r, string moduleSlug, CancellationToken ct)
    {
        if (r.Sections.Select(x => x.Anchor).Distinct().Count() != r.Sections.Length ||
            r.Claims.Select(x => x.Key).Distinct().Count() != r.Claims.Length) throw ApiException.Invalid("Section anchors and claim keys must be unique.");
        foreach (var text in r.Sections.SelectMany(x => new[] { x.Heading, x.Body }).Concat(r.Claims.Select(x => x.Text))
            .Concat(r.MediaPlacements.Select(x => x.Caption ?? "")).Append(r.Title).Append(r.Summary))
            if (Regex.IsMatch(text, @"<\s*/?\s*[a-zA-Z!]", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                throw ApiException.Invalid("Use plain text for article content. HTML is unsupported.");
        var refs = r.ResourceRefs;
        var s = new ArticleSnapshot { ModuleSlug = moduleSlug, ArticleId = articleId, CategorySlug = r.CategorySlug, LanguageCode = r.LanguageCode,
            Title = r.Title.Trim(), Summary = r.Summary.Trim(), Sections = r.Sections, Claims = r.Claims,
            RelatedArticleIds = r.RelatedArticleIds.Where(x => x != articleId).Distinct().ToArray() };
        s.TechnologyProfile = r.TechnologyProfile;
        if (s.TechnologyProfile is { } p)
        {
            var resources = p.Resources.Select(x => x.ResourceId).Distinct().ToArray();
            var hazards = p.Hazards.Select(x => x.HazardId).Distinct().ToArray();
            if (await db.Resources.CountAsync(x => resources.Contains(x.Id) && x.ArchivedAt == null, ct) != resources.Length ||
                await db.Hazards.CountAsync(x => hazards.Contains(x.Id) && x.ArchivedAt == null, ct) != hazards.Length)
                throw ApiException.Invalid("Unknown resource or hazard.");
        }
        s.Measurements = await LoadAsync("measurements", refs.MeasurementIds, ct);
        s.TimelineEvents = await LoadAsync("timeline-events", refs.TimelineEventIds, ct);
        s.MapLayers = await LoadAsync("map-layers", refs.MapLayerIds, ct);
        s.Facts = await LoadAsync("facts", refs.FactIds.Concat(Ids(s.Measurements, "factId")), ct);
        s.Missions = await LoadAsync("missions", refs.MissionIds.Concat(Ids(s.Measurements.Concat(s.TimelineEvents), "missionId")), ct);
        s.Locations = await LoadAsync("locations", refs.LocationIds.Concat(Ids(s.Measurements.Concat(s.MapLayers), "locationId")), ct);
        s.Media = await LoadAsync("media", refs.MediaIds, ct);
        if (r.MediaPlacements.Select(x => x.MediaId).Distinct().Count() != r.MediaPlacements.Length ||
            r.MediaPlacements.Any(x => !refs.MediaIds.Contains(x.MediaId)))
            throw ApiException.Invalid("Each media placement must be unique and reference an item in resourceRefs.mediaIds.");
        s.Media = s.Media.Select((media, index) =>
        {
            var placement = r.MediaPlacements.SingleOrDefault(x => x.MediaId == media.GetProperty("id").GetGuid());
            var node = System.Text.Json.Nodes.JsonNode.Parse(media.GetRawText())!.AsObject();
            node["displayOrder"] = placement?.DisplayOrder ?? index;
            node["caption"] = placement?.Caption;
            return JsonSerializer.SerializeToElement(node, DiscoveryJson.Options);
        }).OrderBy(x => x.GetProperty("displayOrder").GetInt32()).ToArray();
        var dependencies = r.SourceRevisionIds.Concat(r.TechnologyProfile?.SourceRevisionIds ?? []).Concat(r.Claims.SelectMany(x => x.SourceRevisionIds))
            .Concat(Blocks(s).SelectMany(x => Ids(x.Value, "sourceRevisionId"))).Distinct().Order().ToArray();
        var revisions = await db.SourceRevisions.Include(x => x.Source).Where(x => dependencies.Contains(x.Id)).ToListAsync(ct);
        if (revisions.Count != dependencies.Length || dependencies.Contains(Guid.Empty)) throw ApiException.Invalid("A required source revision is missing.");
        s.Sources = revisions.OrderBy(x => x.Id).Select(ToCitation).ToArray();
        if (moduleSlug == ContentModule.Living || moduleSlug == ContentModule.Science && r.EnglishCitations != null)
        {
            s.Sources = s.Sources.Select(c =>
            {
                var display = r.EnglishCitations?.GetValueOrDefault(c.SourceRevisionId);
                if (display == null || string.IsNullOrWhiteSpace(display.Title) ||
                    (c.Credit != null && string.IsNullOrWhiteSpace(display.Credit)) ||
                    (c.UsageNote != null && string.IsNullOrWhiteSpace(display.UsageNote)))
                    throw ApiException.Invalid("English display text is required for each linked source revision.");
                return c with { Title = display.Title, Credit = display.Credit, UsageNote = display.UsageNote };
            }).ToArray();
        }
        else if (r.EnglishCitations != null) throw ApiException.Invalid("English citation overrides require Living on Mars or Science and Technology.");
        return s;
    }
    private async Task<JsonElement[]> LoadAsync(string kind, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var result = new List<JsonElement>();
        foreach (var id in ids.Distinct())
        {
            var r = await content.GetAsync(kind, id, ct);
            if (r.Entity.ArchivedAt != null) throw ApiException.Invalid("Archived resources cannot be used.");
            result.Add(r.Data);
        }
        return result.ToArray();
    }
    private static Citation ToCitation(SourceRevision r)
    {
        var m = DiscoveryJson.Read<SourceMetadata>(r.MetadataSnapshot);
        return new(r.SourceId, r.Id, r.Source.Provider, m.ExternalRecordId ?? r.Source.ExternalRecordId,
            m.Title, m.Url, m.Authors, m.Organization, m.Credit, m.UsageNote, m.Doi, r.RetrievedAt);
    }
    public static IEnumerable<Guid> Ids(IEnumerable<JsonElement> elements, string property) =>
        elements.Where(x => x.TryGetProperty(property, out var id) && id.ValueKind == JsonValueKind.String)
            .Select(x => x.GetProperty(property).GetGuid());
    public static Dictionary<string, JsonElement[]> Blocks(ArticleSnapshot s) => new() {
        ["facts"] = s.Facts, ["measurements"] = s.Measurements, ["missions"] = s.Missions,
        ["locations"] = s.Locations, ["timeline-events"] = s.TimelineEvents, ["media"] = s.Media, ["map-layers"] = s.MapLayers
    };
    public static ArticleSnapshot Read(ArticleVersion v)
    {
        if (v.SnapshotSchemaVersion != 1) throw ApiException.Gate("The snapshot schema must be upgraded.");
        var s = DiscoveryJson.Read<ArticleSnapshot>(v.Snapshot);
        if (s.SchemaVersion != 1 || s.ArticleId == Guid.Empty || s.ArticleId != v.ArticleId) throw ApiException.Gate("This legacy snapshot requires editorial review.");
        return s;
    }
    public async Task ValidateGateAsync(ArticleVersion v, CancellationToken ct)
    {
        var s = Read(v);
        if (s.ModuleSlug == ContentModule.Survival && (s.LanguageCode != "en" || v.LanguageCode != "en"))
            throw ApiException.Gate("Only English Survival Knowledge versions can be submitted or published.");
        if (s.ModuleSlug == ContentModule.Living && (s.LanguageCode != "en" || v.LanguageCode != "en" ||
            !await db.LivingEnglishReviews.AnyAsync(r => r.ArticleVersionId == v.Id && r.SnapshotHash == v.SnapshotHash, ct)))
            throw ApiException.Gate("English review of all snapshot fields is required.");
        if (v.SnapshotHash != SourceNormalization.Hash(v.Snapshot)) throw ApiException.Gate("The snapshot hash does not match.");
        var dependencies = s.Sources.Select(x => x.SourceRevisionId).ToHashSet();
        var links = await db.ArticleVersionSources.Where(x => x.ArticleVersionId == v.Id).ToListAsync(ct);
        if (dependencies.Count == 0 || !dependencies.SetEquals(links.Select(x => x.SourceRevisionId)))
            throw ApiException.Gate("Source revision links differ from the snapshot.");
        if (s.ModuleSlug == ContentModule.Science)
        {
            var p = s.TechnologyProfile;
            if (s.LanguageCode != "en" || p == null || !Enum.IsDefined(p.TechnologyStatus) ||
                p.EvidenceType == EvidenceType.Unspecified || p.TestEnvironment == TestEnvironment.Unspecified ||
                string.IsNullOrWhiteSpace(p.Capabilities) || string.IsNullOrWhiteSpace(p.Limitations) ||
                !Enum.IsDefined(p.EvidenceType) || !Enum.IsDefined(p.TestEnvironment) ||
                p.SourceRevisionIds.Length == 0 || !p.SourceRevisionIds.All(dependencies.Contains) ||
                !s.Sections.Any(x => x.Anchor == "problem") || !s.Sections.Any(x => x.Anchor == "principle") ||
                s.Facts.Any(f => string.IsNullOrWhiteSpace(p.FactLocators?.GetValueOrDefault(f.GetProperty("id").GetGuid())) ||
                    Text(f, "verificationStatus") != "Accepted" || Text(f, "sourceRevisionId") == null ||
                    string.IsNullOrWhiteSpace(Text(f, "originalUnit")) || Text(f, "originalValue") == null))
                throw ApiException.Gate("A sourced technical profile, problem and principle sections, and accepted facts with original units are required.");
        }
        var actual = s.Claims.SelectMany(x => x.SourceRevisionIds).Concat(Blocks(s).SelectMany(x => Ids(x.Value, "sourceRevisionId"))).Distinct();
        if (actual.Any(x => !dependencies.Contains(x)) || s.Sources.Any(x => !links.Any(l => l.SourceId == x.SourceId && l.SourceRevisionId == x.SourceRevisionId)))
            throw ApiException.Gate("A dependency is missing or its source does not match.");
        if (await db.SourceRevisions.AnyAsync(x => dependencies.Contains(x.Id) &&
            (x.VerificationStatus != VerificationStatus.Accepted || x.RevokedAt != null || x.VerifiedAt == null || x.VerifiedById == null), ct))
            throw ApiException.Gate("Every source revision must be accepted and active.");
        if (s.Claims.Length == 0 || s.Claims.Any(x => x.SourceRevisionIds.Length == 0 ||
            x.EvidenceType == EvidenceType.Unspecified || x.TestEnvironment == TestEnvironment.Unspecified ||
            x.Locator is null || x.Locator.Count == 0 || x.Locator.Values.Any(string.IsNullOrWhiteSpace)))
            throw ApiException.Gate("Each claim requires a source revision, locator, evidence type and context.");
        if (s.ModuleSlug == ContentModule.Survival && s.Facts.Any(f => Text(f, "verificationStatus") != "Accepted"))
            throw ApiException.Gate("Approve each fact before saving a Survival Knowledge draft that uses it.");
        foreach (var f in s.Facts.Concat(s.Measurements))
            if (Text(f, "evidenceType") is null or "Unspecified" || Text(f, "testEnvironment") is null or "Unspecified" ||
                !f.TryGetProperty("unit", out _) || string.IsNullOrWhiteSpace(Text(f, "context")))
                throw ApiException.Gate("A fact or measurement is missing its unit, context or evidence type.");
        foreach (var m in s.Measurements)
            if (Text(m, "qualityFlag") is not ("valid" or "good" or "0") || string.IsNullOrWhiteSpace(Text(m, "instrument")))
                throw ApiException.Gate("Verify the measurement quality flag and instrument.");
        foreach (var m in s.Media)
        {
            if (new[] { "credit", "usageNote", "altText", "url", "originalUrl" }.Any(x => string.IsNullOrWhiteSpace(Text(m, x))))
                throw ApiException.Gate("Media requires credit, usage rights, alternative text and URLs.");
            if (Text(m, "fileChecksum") is { } checksum)
            {
                if (Text(m, "mimeType") is not ("image/jpeg" or "image/png" or "image/gif" or "image/webp" or "image/tiff"))
                    throw ApiException.Gate("Stored images require a supported MIME type.");
                try { _ = await raw.ReadAsync(checksum, ct); }
                catch (Exception e) when (e is ApiException or IOException) { throw ApiException.Gate("The stored image could not be verified against its checksum."); }
            }
        }
        if (s.Missions.Any(m => Text(m, "statusVerifiedAt") is null))
            throw ApiException.Gate("The mission requires a status verification date.");
        foreach (var layer in s.MapLayers)
            if (new[] { "attribution", "style", "coordinateSystem", "boundingBoxJson", "resourceUrlTemplate", "tileMatrixSet" }
                .Any(x => string.IsNullOrWhiteSpace(Text(layer, x))))
                throw ApiException.Gate("Map layers require attribution, projection, bounds and complete WMTS metadata.");
    }
    public static string? Text(JsonElement e, string key) =>
        e.TryGetProperty(key, out var p) && p.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) ? p.ToString() : null;
    public void AddProjection(ArticleVersion v, ArticleSnapshot s)
    {
        foreach (var (kind, values) in Blocks(s))
        foreach (var value in values)
        {
            Guid? Id(string key) => Guid.TryParse(Text(value, key), out var id) ? id : null;
            decimal? Number(string key) => value.TryGetProperty(key, out var p) && p.TryGetDecimal(out var n) ? n : null;
            db.PublicationResources.Add(new() {
                ArticleVersionId = v.Id, Kind = kind, ResourceId = value.GetProperty("id").GetGuid(), Payload = value.GetRawText(),
                Name = Text(value, "name") ?? Text(value, "title") ?? "", Status = Text(value, "status"), Type = Text(value, "missionType"),
                QuantityKind = value.TryGetProperty("unit", out var unit) ? Text(unit, "quantityKind") : null,
                EvidenceType = Text(value, "evidenceType"), MissionId = Id("missionId"), LocationId = Id("locationId"), FactId = Id("factId"),
                Date = DateTimeOffset.TryParse(Text(value, "eventDate") ?? Text(value, "observedAt"), out var date) ? date : null,
                IsPlanned = Text(value, "isPlanned") == "True", Latitude = Number("latitude"), Longitude = Number("longitude")
            });
        }
    }
}
