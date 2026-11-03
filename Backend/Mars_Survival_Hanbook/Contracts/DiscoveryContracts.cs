using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Contracts;

public static class DiscoveryJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Options)
        ?? throw new JsonException("Missing JSON value.");
}

public record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public class PageQuery
{
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [MaxLength(200)] public string? Q { get; set; }
    [RegularExpression("^[a-z]{2}(-[A-Za-z]{2})?$")] public string Language { get; set; } = "vi";
    public int Skip => checked((Page - 1) * PageSize);
    public string? Search => Q?.Trim().Normalize();
}
public class DiscoveryQuery : PageQuery
{
    [MaxLength(180)] public string? Category { get; set; }
    [RegularExpression("^(relevance|publishedAtDesc)$")] public string Sort { get; set; } = "publishedAtDesc";
    public Guid? MissionId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? FactId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public bool IncludePlanned { get; set; }
    public string? Bbox { get; set; }
    public MissionStatus? Status { get; set; }
    public MissionType? Type { get; set; }
    [MaxLength(100)] public string? QuantityKind { get; set; }
    public EvidenceType? EvidenceType { get; set; }
}
public record LoginRequest([Required, EmailAddress, MaxLength(254)] string Email, [Required, MaxLength(1024)] string Password);
public record SourceAuthor([Required, MaxLength(200)] string Name, [Required, MaxLength(300)] string Organization);
public record SourceMetadata(
    string Title, string Url, SourceAuthor[] Authors, string Organization, SourceType SourceType,
    string? Excerpt = null, string? UsageNote = null, string? Credit = null, string? Doi = null,
    string? ExternalRecordId = null, DateTimeOffset? PublishedAt = null,
    JsonElement? ProviderMetadata = null, Dictionary<string, string>? AttachmentHashes = null);
public record ManualSourceRequest(
    [Required, MaxLength(2048)] string Url,
    [Required, MaxLength(500)] string Title,
    [Required, MinLength(1), MaxLength(100)] SourceAuthor[] Authors,
    [Required, MaxLength(300)] string Organization,
    SourceType SourceType,
    [Required, MaxLength(200000)] string Excerpt,
    [Required, MaxLength(4000)] string UsageNote,
    DateTimeOffset RetrievedAt,
    [MaxLength(1000)] string? Credit = null);
public record SourceDecision(VerificationStatus Decision, [Required, MaxLength(4000)] string Notes);
public record SourceAmendmentRequest(
    [Required, MaxLength(500)] string Title, [Required, MinLength(1), MaxLength(100)] SourceAuthor[] Authors,
    [Required, MaxLength(300)] string Organization, [Required, MaxLength(200000)] string Excerpt,
    [Required, MaxLength(4000)] string UsageNote, [MaxLength(1000)] string? Credit);
public record ReasonRequest([Required, MaxLength(4000)] string Reason);
public record VersionRequest(Guid VersionId);
public record RestoreRequest(Guid SourceVersionId, [Required, MaxLength(2000)] string ChangeSummary);
public record SectionInput(
    [Required, RegularExpression("^[a-z0-9-]{1,100}$")] string Anchor,
    [Required, MaxLength(300)] string Heading,
    [Required, MaxLength(100000)] string Body);
public record ClaimInput(
    [Required, MaxLength(100)] string Key,
    [Required, MaxLength(4000)] string Text,
    EvidenceType EvidenceType, TestEnvironment TestEnvironment,
    [Required, MaxLength(30)] Guid[] SourceRevisionIds,
    Dictionary<string, string>? Locator);
public class ResourceRefs
{
    [MaxLength(100)] public Guid[] FactIds { get; set; } = [];
    [MaxLength(100)] public Guid[] MeasurementIds { get; set; } = [];
    [MaxLength(100)] public Guid[] MissionIds { get; set; } = [];
    [MaxLength(100)] public Guid[] LocationIds { get; set; } = [];
    [MaxLength(100)] public Guid[] TimelineEventIds { get; set; } = [];
    [MaxLength(100)] public Guid[] MediaIds { get; set; } = [];
    [MaxLength(100)] public Guid[] MapLayerIds { get; set; } = [];
}
public class ArticleDraftRequest
{
    [Required, MaxLength(180)] public string CategorySlug { get; set; } = "";
    [Required, RegularExpression("^[a-z0-9-]{1,200}$")] public string Slug { get; set; } = "";
    [Required, RegularExpression("^[a-z]{2}(-[A-Za-z]{2})?$")] public string LanguageCode { get; set; } = "vi";
    [Required, MaxLength(300)] public string Title { get; set; } = "";
    [Required, MaxLength(2000)] public string Summary { get; set; } = "";
    [Required, MinLength(1), MaxLength(100)] public SectionInput[] Sections { get; set; } = [];
    [Required, MaxLength(200)] public ClaimInput[] Claims { get; set; } = [];
    [Required] public ResourceRefs ResourceRefs { get; set; } = new();
    [Required, MaxLength(100)] public MediaPlacement[] MediaPlacements { get; set; } = [];
    [Required, MaxLength(100)] public Guid[] SourceRevisionIds { get; set; } = [];
    [Required, MaxLength(50)] public Guid[] RelatedArticleIds { get; set; } = [];
    [MaxLength(2000)] public string? ChangeSummary { get; set; }
}
public record MediaPlacement(Guid MediaId, [Range(0, 10000)] int DisplayOrder, [MaxLength(2000)] string? Caption);
public record Citation(Guid SourceId, Guid SourceRevisionId, SourceProvider Provider,
    string? ExternalRecordId, string Title, string Url, SourceAuthor[] Authors, string Organization,
    string? Credit, string? UsageNote, string? Doi, DateTimeOffset RetrievedAt);
public class ArticleSnapshot
{
    public int SchemaVersion { get; set; } = 1;
    public Guid ArticleId { get; set; }
    public string ModuleSlug { get; set; } = "discover-mars";
    public string CategorySlug { get; set; } = "";
    public string LanguageCode { get; set; } = "vi";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public SectionInput[] Sections { get; set; } = [];
    public ClaimInput[] Claims { get; set; } = [];
    public JsonElement[] Facts { get; set; } = [];
    public JsonElement[] Measurements { get; set; } = [];
    public JsonElement[] Missions { get; set; } = [];
    public JsonElement[] Locations { get; set; } = [];
    public JsonElement[] TimelineEvents { get; set; } = [];
    public JsonElement[] Media { get; set; } = [];
    public JsonElement[] MapLayers { get; set; } = [];
    public Citation[] Sources { get; set; } = [];
    public Guid[] RelatedArticleIds { get; set; } = [];
}
public record ReviewChecklist(bool SourcesChecked, bool ClaimsChecked, bool UnitsChecked,
    bool EvidenceChecked, bool MediaRightsChecked)
{
    public bool Complete => SourcesChecked && ClaimsChecked && UnitsChecked && EvidenceChecked && MediaRightsChecked;
}
public record ReviewDecision(ReviewState Decision, [Required, MaxLength(4000)] string Notes,
    [Required] ReviewChecklist Checklist);
public class ImportRequest
{
    [Required, MaxLength(100)] public string ConnectorKey { get; set; } = "";
    [MaxLength(200)] public string? Query { get; set; }
    [MaxLength(100)] public string[] RecordIds { get; set; } = [];
    [Range(1, 500)] public int MaxItems { get; set; } = 20;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [MaxLength(100)] public string? LayerKey { get; set; }
    public bool IncludeMembers { get; set; }
    [MaxLength(100)] public string PdsCollection { get; set; } = "data_calibrated_env";
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
public record ConnectorRequest(bool IsEnabled, [Required, MaxLength(2048)] string BaseUrl,
    [Required, MaxLength(4000)] string AccessPolicy, [Range(1, 1000)] int? RateLimitPerMinute);
