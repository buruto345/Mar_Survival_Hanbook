using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>A reader report; anonymous submissions are supported.</summary>
public class FeedbackReport : Entity
{
    public FeedbackReason Reason { get; set; } = FeedbackReason.Other;

    [MaxLength(4000)]
    public required string Comment { get; set; }

    public FeedbackStatus Status { get; set; } = FeedbackStatus.Open;

    public DateTimeOffset? ResolvedAt { get; set; }

    [MaxLength(4000)]
    public string? ResolutionNotes { get; set; }

    public Guid ArticleId { get; set; }

    public Guid? ReportedById { get; set; }

    public Guid? ResolvedById { get; set; }

    public Article Article { get; set; } = null!;

    public User? ReportedBy { get; set; }

    public User? ResolvedBy { get; set; }
}

