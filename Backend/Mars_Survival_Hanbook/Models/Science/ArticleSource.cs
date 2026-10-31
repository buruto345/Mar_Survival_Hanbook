using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Links an article's claim scope to its NASA source and evidence context.</summary>
public class ArticleSource
{
    [MaxLength(4000)]
    public required string ClaimNote { get; set; }

    public EvidenceType EvidenceType { get; set; } = EvidenceType.Unspecified;

    public TestEnvironment TestEnvironment { get; set; } = TestEnvironment.Unspecified;

    public Guid ArticleId { get; set; }

    public Guid SourceId { get; set; }

    public Article Article { get; set; } = null!;

    public ScientificSource Source { get; set; } = null!;
}

