using System.ComponentModel.DataAnnotations;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Describes how a technology mitigates a hazard without implying complete protection.</summary>
public class TechnologyHazard
{
    [MaxLength(4000)]
    public required string MitigationDescription { get; set; }

    public Guid TechnologyId { get; set; }

    public Guid HazardId { get; set; }

    public Technology Technology { get; set; } = null!;

    public Hazard Hazard { get; set; } = null!;
}

