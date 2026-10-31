using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Measurement unit and physical quantity; conversions require domain-aware application logic.</summary>
public class Unit : Entity
{
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(32)]
    public required string Symbol { get; set; }

    [MaxLength(100)]
    public required string QuantityKind { get; set; }

    public bool IsSi { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public ICollection<ScientificFact> Facts { get; set; } = new List<ScientificFact>();

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
}

