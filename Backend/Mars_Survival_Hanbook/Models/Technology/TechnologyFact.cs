using System.ComponentModel.DataAnnotations;

namespace Mars_Survival_Hanbook.Models;

/// <summary>A sourced technology comparison criterion; compare only compatible units and conditions.</summary>
public class TechnologyFact
{
    [MaxLength(150)]
    public required string Criterion { get; set; }

    [MaxLength(2000)]
    public required string ComparisonContext { get; set; }

    public Guid TechnologyId { get; set; }

    public Guid FactId { get; set; }

    public Technology Technology { get; set; } = null!;

    public ScientificFact Fact { get; set; } = null!;
}

