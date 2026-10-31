using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>An optional Mars Trek WMTS layer with independently reviewed NASA provenance.</summary>
public class MapLayer : Entity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(2048)]
    public required string ServiceUrl { get; set; }

    [MaxLength(250)]
    public required string LayerIdentifier { get; set; }

    [MaxLength(200)]
    public required string TileMatrixSet { get; set; }

    [MaxLength(100)]
    public required string Format { get; set; }

    [MaxLength(1000)]
    public required string Attribution { get; set; }

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

    public Guid SourceId { get; set; }

    public ScientificSource Source { get; set; } = null!;
}

