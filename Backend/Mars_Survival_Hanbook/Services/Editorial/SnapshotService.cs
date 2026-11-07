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
    public async Task<ArticleSnapshot> BuildAsync(Guid articleId, ArticleDraftRequest r, CancellationToken ct)
    {
        if (r.Sections.Select(x => x.Anchor).Distinct().Count() != r.Sections.Length ||
            r.Claims.Select(x => x.Key).Distinct().Count() != r.Claims.Length) throw ApiException.Invalid("Anchor/claim key phải duy nhất.");
        foreach (var text in r.Sections.SelectMany(x => new[] { x.Heading, x.Body }).Concat(r.Claims.Select(x => x.Text))
            .Concat(r.MediaPlacements.Select(x => x.Caption ?? "")).Append(r.Title).Append(r.Summary))
            if (Regex.IsMatch(text, @"<\s*/?\s*[a-zA-Z!]", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                throw ApiException.Invalid("Nội dung dùng văn bản thuần; không nhận HTML.");
        var refs = r.ResourceRefs;
        var s = new ArticleSnapshot { ArticleId = articleId, CategorySlug = r.CategorySlug, LanguageCode = r.LanguageCode,
            Title = r.Title.Trim(), Summary = r.Summary.Trim(), Sections = r.Sections, Claims = r.Claims,
            RelatedArticleIds = r.RelatedArticleIds.Where(x => x != articleId).Distinct().ToArray() };
        s.Measurements = await LoadAsync("measurements", refs.MeasurementIds, ct);
        s.TimelineEvents = await LoadAsync("timeline-events", refs.TimelineEventIds, ct);
        s.MapLayers = await LoadAsync("map-layers", refs.MapLayerIds, ct);
        s.Facts = await LoadAsync("facts", refs.FactIds.Concat(Ids(s.Measurements, "factId")), ct);
        s.Missions = await LoadAsync("missions", refs.MissionIds.Concat(Ids(s.Measurements.Concat(s.TimelineEvents), "missionId")), ct);
        s.Locations = await LoadAsync("locations", refs.LocationIds.Concat(Ids(s.Measurements.Concat(s.MapLayers), "locationId")), ct);
        s.Media = await LoadAsync("media", refs.MediaIds, ct);
        if (r.MediaPlacements.Select(x => x.MediaId).Distinct().Count() != r.MediaPlacements.Length ||
            r.MediaPlacements.Any(x => !refs.MediaIds.Contains(x.MediaId)))
            throw ApiException.Invalid("Media placement phải duy nhất và thuộc resourceRefs.mediaIds.");
        s.Media = s.Media.Select((media, index) =>
        {
            var placement = r.MediaPlacements.SingleOrDefault(x => x.MediaId == media.GetProperty("id").GetGuid());
            var node = System.Text.Json.Nodes.JsonNode.Parse(media.GetRawText())!.AsObject();
            node["displayOrder"] = placement?.DisplayOrder ?? index;
            node["caption"] = placement?.Caption;
            return JsonSerializer.SerializeToElement(node, DiscoveryJson.Options);
        }).OrderBy(x => x.GetProperty("displayOrder").GetInt32()).ToArray();
        var dependencies = r.SourceRevisionIds.Concat(r.Claims.SelectMany(x => x.SourceRevisionIds))
            .Concat(Blocks(s).SelectMany(x => Ids(x.Value, "sourceRevisionId"))).Distinct().Order().ToArray();
        var revisions = await db.SourceRevisions.Include(x => x.Source).Where(x => dependencies.Contains(x.Id)).ToListAsync(ct);
        if (revisions.Count != dependencies.Length || dependencies.Contains(Guid.Empty)) throw ApiException.Invalid("Thiếu source revision của nội dung.");
        s.Sources = revisions.OrderBy(x => x.Id).Select(ToCitation).ToArray();
        return s;
    }
    private async Task<JsonElement[]> LoadAsync(string kind, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var result = new List<JsonElement>();
        foreach (var id in ids.Distinct())
        {
            var r = await content.GetAsync(kind, id, ct);
            if (r.Entity.ArchivedAt != null) throw ApiException.Invalid("Không dùng working record đã lưu trữ.");
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
        if (v.SnapshotSchemaVersion != 1) throw ApiException.Gate("Snapshot cần chuyển đổi schema trước khi sử dụng.");
        var s = DiscoveryJson.Read<ArticleSnapshot>(v.Snapshot);
        if (s.SchemaVersion != 1 || s.ArticleId == Guid.Empty || s.ArticleId != v.ArticleId) throw ApiException.Gate("Snapshot legacy cần biên tập lại.");
        return s;
    }
    public async Task ValidateGateAsync(ArticleVersion v, CancellationToken ct)
    {
        var s = Read(v);
        if (v.SnapshotHash != SourceNormalization.Hash(v.Snapshot)) throw ApiException.Gate("Snapshot hash không khớp.");
        var dependencies = s.Sources.Select(x => x.SourceRevisionId).Order().ToArray();
        var links = await db.ArticleVersionSources.Where(x => x.ArticleVersionId == v.Id).OrderBy(x => x.SourceRevisionId).ToListAsync(ct);
        if (dependencies.Length == 0 || !dependencies.SequenceEqual(links.Select(x => x.SourceRevisionId)))
            throw ApiException.Gate("Dependency links không khớp snapshot.");
        var actual = s.Claims.SelectMany(x => x.SourceRevisionIds).Concat(Blocks(s).SelectMany(x => Ids(x.Value, "sourceRevisionId"))).Distinct();
        if (actual.Any(x => !dependencies.Contains(x)) || s.Sources.Any(x => !links.Any(l => l.SourceId == x.SourceId && l.SourceRevisionId == x.SourceRevisionId)))
            throw ApiException.Gate("Thiếu dependency hoặc source parent không khớp.");
        if (await db.SourceRevisions.AnyAsync(x => dependencies.Contains(x.Id) &&
            (x.VerificationStatus != VerificationStatus.Accepted || x.RevokedAt != null || x.VerifiedAt == null || x.VerifiedById == null), ct))
            throw ApiException.Gate("Mọi revision phải được reviewer chấp nhận và chưa thu hồi.");
        if (s.Claims.Length == 0 || s.Claims.Any(x => x.SourceRevisionIds.Length == 0 ||
            x.EvidenceType == EvidenceType.Unspecified || x.TestEnvironment == TestEnvironment.Unspecified ||
            x.Locator is null || x.Locator.Count == 0 || x.Locator.Values.Any(string.IsNullOrWhiteSpace)))
            throw ApiException.Gate("Claim cần nguồn, locator, evidence và bối cảnh rõ ràng.");
        foreach (var f in s.Facts.Concat(s.Measurements))
            if (Text(f, "evidenceType") is null or "Unspecified" || Text(f, "testEnvironment") is null or "Unspecified" ||
                !f.TryGetProperty("unit", out _) || string.IsNullOrWhiteSpace(Text(f, "context")))
                throw ApiException.Gate("Fact/measurement thiếu đơn vị, bối cảnh hoặc evidence.");
        foreach (var m in s.Measurements)
            if (Text(m, "qualityFlag") is not ("valid" or "good" or "0") || string.IsNullOrWhiteSpace(Text(m, "instrument")))
                throw ApiException.Gate("Measurement chưa qua kiểm tra quality/instrument.");
        foreach (var m in s.Media)
        {
            if (new[] { "credit", "usageNote", "altText", "url", "originalUrl" }.Any(x => string.IsNullOrWhiteSpace(Text(m, x))))
                throw ApiException.Gate("Media thiếu credit/quyền sử dụng/alt text.");
            if (Text(m, "fileChecksum") is { } checksum)
            {
                if (Text(m, "mimeType") is not ("image/jpeg" or "image/png" or "image/gif" or "image/webp" or "image/tiff"))
                    throw ApiException.Gate("File ảnh lưu trữ cần MIME được hỗ trợ.");
                try { _ = await raw.ReadAsync(checksum, ct); }
                catch (Exception e) when (e is ApiException or IOException) { throw ApiException.Gate("Không tìm thấy file ảnh đúng checksum."); }
            }
        }
        if (s.Missions.Any(m => Text(m, "statusVerifiedAt") is null))
            throw ApiException.Gate("Mission cần ngày xác minh trạng thái.");
        foreach (var layer in s.MapLayers)
            if (new[] { "attribution", "style", "coordinateSystem", "boundingBoxJson", "resourceUrlTemplate", "tileMatrixSet" }
                .Any(x => string.IsNullOrWhiteSpace(Text(layer, x))))
                throw ApiException.Gate("Map layer cần attribution, projection, bbox và WMTS metadata đầy đủ.");
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
