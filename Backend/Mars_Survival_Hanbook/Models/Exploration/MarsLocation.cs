using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Named Mars location. Longitude uses the -180 to 180 convention.</summary>
public class MarsLocation : Entity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(200)]
    public required string Slug { get; set; }

    [Precision(10, 7)]
    public decimal Latitude { get; set; }

    [Precision(10, 7)]
    public decimal Longitude { get; set; }

    [MaxLength(150)]
    public required string CoordinateSystem { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    public Guid SourceId { get; set; }

    public ScientificSource Source { get; set; } = null!;

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
}

