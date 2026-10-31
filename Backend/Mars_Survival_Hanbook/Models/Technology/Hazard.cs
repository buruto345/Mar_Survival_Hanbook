using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>An environmental or human-survival hazard with NASA provenance.</summary>
public class Hazard : Entity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(200)]
    public required string Slug { get; set; }

    public required string Description { get; set; }

    public EvidenceType EvidenceType { get; set; } = EvidenceType.Unspecified;

    [MaxLength(4000)]
    public string? Limitations { get; set; }

    public Guid SourceId { get; set; }

    public ScientificSource Source { get; set; } = null!;

    public ICollection<ArticleHazard> Articles { get; set; } = new List<ArticleHazard>();

    public ICollection<TechnologyHazard> Technologies { get; set; } = new List<TechnologyHazard>();
}

