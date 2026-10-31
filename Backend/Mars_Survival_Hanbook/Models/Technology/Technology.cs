using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Technology record with capabilities, limitations, process and separately sourced facts.</summary>
public class Technology : Entity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(200)]
    public required string Slug { get; set; }

    public required string Description { get; set; }

    public TechnologyStatus Status { get; set; } = TechnologyStatus.Proposed;

    public EvidenceType EvidenceType { get; set; } = EvidenceType.Unspecified;

    public TestEnvironment EnvironmentTested { get; set; } = TestEnvironment.Unspecified;

    [MaxLength(4000)]
    public required string Capabilities { get; set; }

    [MaxLength(4000)]
    public required string Limitations { get; set; }

    public string? ProcessDescription { get; set; }

    public DateTimeOffset? VerifiedAt { get; set; }

    public Guid? VerifiedById { get; set; }

    public User? VerifiedBy { get; set; }

    public ICollection<ArticleTechnology> Articles { get; set; } = new List<ArticleTechnology>();

    public ICollection<TechnologySource> Sources { get; set; } = new List<TechnologySource>();

    public ICollection<TechnologyFact> Facts { get; set; } = new List<TechnologyFact>();

    public ICollection<TechnologyResource> Resources { get; set; } = new List<TechnologyResource>();

    public ICollection<TechnologyHazard> Hazards { get; set; } = new List<TechnologyHazard>();
}

