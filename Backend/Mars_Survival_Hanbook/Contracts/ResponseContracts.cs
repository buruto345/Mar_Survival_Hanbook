using System.Text.Json;
using Mars_Survival_Hanbook.Models.Enums;
namespace Mars_Survival_Hanbook.Contracts;

public record ArticleCard(Guid Id, string Slug, string Title, string Summary, string CategorySlug, string LanguageCode,
    Guid ArticleVersionId, int Version, DateTimeOffset? ReviewedAt, DateTimeOffset? PublishedAt, string? ThumbnailUrl);
public record TechnologyCard(Guid Id, string Slug, string Title, string Summary, string CategorySlug, string TechnologyStatus, string TestEnvironment);
public record HumanCategoryResponse(Guid Id, string Slug, string Name, string? Description, int PublishedArticleCount, string? ArticleSlug);
public record HumanOverviewResponse(Guid Id, string Name, string Slug, string? Description, HumanCategoryResponse[] Categories, int PublishedArticleCount, string? CoverUrl);
public record CategoryResponse(Guid Id, string Slug, string Name, string? Description, int PublishedArticleCount);
public record ModuleResponse(Guid Id, string Name, string Slug, string? Description, CategoryResponse[] Categories,
    int PublishedArticleCount, string? CoverUrl);
public record LoginResponse(string AccessToken, string TokenType, int ExpiresIn, UserProfile Profile);
public record UserProfile(Guid Id, string Username, string? FullName, string? Email, UserRole Role);
public record ArticleAdminResponse(Guid Id, string Title, string Slug, string Summary, Guid CategoryId, string LanguageCode,
    ArticleStatus Status, Guid? PublishedVersionId, Guid? CurrentDraftVersionId, bool RequiresReview, Guid CreatedById,
    DateTimeOffset? ReviewedAt, DateTimeOffset? PublishedAt);
public record ArticleVersionResponse(Guid Id, Guid ArticleId, int Version, ArticleVersionReviewState ReviewState,
    string SnapshotHash, int SnapshotSchemaVersion, ArticleSnapshot Snapshot, DateTimeOffset ChangedAt,
    DateTimeOffset? ReviewedAt, Guid? ReviewedById);
public record ArticleVersionSummary(Guid Id, int Version, ArticleVersionReviewState ReviewState, int SnapshotSchemaVersion,
    string SnapshotHash, string? ChangeSummary, DateTimeOffset ChangedAt, DateTimeOffset? ReviewedAt);
public record RelatedArticle(Guid Id, string Slug, string Title, Guid ArticleVersionId, string? Url = null, string? LanguageCode = null);
public record TableOfContentsEntry(string Anchor, string Heading);
public sealed class ArticleDetailResponse : ArticleSnapshot
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public Guid ArticleVersionId { get; set; }
    public int Version { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? SurvivalKnowledgeUrl { get; set; }
    public string BodyFormat { get; set; } = "plainText";
    public TableOfContentsEntry[] TableOfContents { get; set; } = [];
    public RelatedArticle[] RelatedArticles { get; set; } = [];
}
public record ReviewResponse(Guid Id, Guid ArticleId, Guid ArticleVersionId, ReviewState State, Guid? ReviewerId,
    Guid SubmittedById, DateTimeOffset SubmittedAt, DateTimeOffset? CompletedAt, DateTimeOffset? DueAt, string? ReviewNotes,
    ReviewChecklist Checklist);
public record ImportResponse(Guid Id, Guid JobId, Guid ConnectorId, IngestionStatus Status,
    DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, int RecordsFetched, int RecordsAccepted, int RecordsUnchanged,
    int RecordsRejected, int RecordsFailed, string? ErrorMessage, int AttemptCount, string? CheckpointJson,
    DateTimeOffset? LeaseExpiresAt, DateTimeOffset? NextAttemptAt);
public record ImportItemResponse(Guid Id, string ItemKey, IngestionItemStatus Status, Guid? SourceId, Guid? SourceRevisionId,
    int AttemptCount, DateTimeOffset? NextAttemptAt, string? ErrorCode, string? ErrorMessage, string? NormalizedPayload);
public record ConnectorResponse(Guid Id, string Key, string Name, ConnectorType ConnectorType, string BaseUrl,
    bool IsEnabled, string? AccessPolicy, int? RateLimitPerMinute, DateTimeOffset? LastSyncedAt, string ETag);
public record SourceResponse(Guid Id, SourceProvider Provider, string IdentityKey, string? ExternalRecordId, string Title,
    Guid? CurrentAcceptedRevisionId, DateTimeOffset? LastCheckedAt);
public record SourceListResponse(Guid Id, SourceProvider Provider, string IdentityKey, string Title,
    Guid? CurrentAcceptedRevisionId, VerificationStatus VerificationStatus);
public record SourceRevisionResponse(Guid Id, Guid SourceId, int RevisionNumber, string? ProviderVersion,
    string ContentHash, string Fingerprint, string NormalizationVersion, SourceMetadata Metadata, string? RawChecksum,
    DateTimeOffset RetrievedAt, VerificationStatus VerificationStatus, Guid? VerifiedById, DateTimeOffset? VerifiedAt,
    string? VerificationNotes, DateTimeOffset? RevokedAt, string? RevocationReason);
public record SourceRevisionSummary(Guid Id, Guid SourceId, int RevisionNumber, string Fingerprint,
    VerificationStatus VerificationStatus, DateTimeOffset? RevokedAt, DateTimeOffset RetrievedAt);
