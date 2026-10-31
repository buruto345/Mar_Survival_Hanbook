using System.ComponentModel.DataAnnotations;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Explicit association with a composite key.</summary>
public class TechnologySource
{
    [MaxLength(4000)]
    public required string ClaimNote { get; set; }

    public Guid TechnologyId { get; set; }

    public Guid SourceId { get; set; }

    public Technology Technology { get; set; } = null!;

    public ScientificSource Source { get; set; } = null!;
}

