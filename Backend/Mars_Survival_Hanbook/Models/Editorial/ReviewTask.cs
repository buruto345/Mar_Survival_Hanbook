using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Scientific review of a specific article version, preventing ambiguity after subsequent edits.</summary>
public class ReviewTask : Entity
{
    public ReviewState State { get; set; } = ReviewState.Pending;

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ReviewNotes { get; set; }

    public bool SourcesChecked { get; set; }

    public bool ClaimsChecked { get; set; }

    public bool UnitsChecked { get; set; }

    public bool EvidenceChecked { get; set; }

    public bool MediaRightsChecked { get; set; }

    public Guid ArticleId { get; set; }

    public Guid ArticleVersionId { get; set; }

    public Guid SubmittedById { get; set; }

    public Guid? ReviewerId { get; set; }

    public Article Article { get; set; } = null!;

    public ArticleVersion ArticleVersion { get; set; } = null!;

    public User SubmittedBy { get; set; } = null!;

    public User? Reviewer { get; set; }
}

