using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>An immutable snapshot to be appended by the publishing or editing service.</summary>
public class ArticleVersion : Entity
{
    public int SnapshotSchemaVersion { get; set; } = 1;
    [MaxLength(64)] public string SnapshotHash { get; set; } = "";
    public Mars_Survival_Hanbook.Models.Enums.ArticleVersionReviewState ReviewState { get; set; }
    public Guid? ReviewedById { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    [MaxLength(300)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string Summary { get; set; } = "";
    [MaxLength(180)] public string CategorySlug { get; set; } = "";
    [MaxLength(10)] public string LanguageCode { get; set; } = "vi";
    public string SearchText { get; set; } = "";
    public int Version { get; set; }

    public required string Snapshot { get; set; }

    [MaxLength(2000)]
    public string? ChangeSummary { get; set; }

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ArticleId { get; set; }

    public Guid ChangedById { get; set; }

    public Article Article { get; set; } = null!;

    public User ChangedBy { get; set; } = null!;

    public ICollection<ReviewTask> Reviews { get; set; } = new List<ReviewTask>();
}

