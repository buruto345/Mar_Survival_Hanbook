using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>An individual observation linked to a fact, source, unit and optional mission/location.</summary>
public class Measurement : Entity
{
    [Precision(28, 12)]
    public decimal Value { get; set; }

    [Precision(28, 12)]
    public decimal? UpperValue { get; set; }

    [Precision(28, 12)]
    public decimal OriginalValue { get; set; }

    [MaxLength(64)]
    public required string OriginalUnit { get; set; }

    public MeasurementKind MeasurementKind { get; set; } = MeasurementKind.Unspecified;

    public DateTimeOffset ObservedAt { get; set; }

    public DateTimeOffset? ObservationEndAt { get; set; }

    [MaxLength(200)]
    public string? Instrument { get; set; }

    [MaxLength(4000)]
    public required string Conditions { get; set; }

    [MaxLength(2000)]
    public string? Uncertainty { get; set; }

    public EvidenceType EvidenceType { get; set; } = EvidenceType.Unspecified;

    public TestEnvironment TestEnvironment { get; set; } = TestEnvironment.Unspecified;

    public DateTimeOffset? VerifiedAt { get; set; }

    public Guid FactId { get; set; }

    public Guid SourceId { get; set; }

    public Guid UnitId { get; set; }

    public Guid? MissionId { get; set; }

    public Guid? LocationId { get; set; }

    public ScientificFact Fact { get; set; } = null!;

    public ScientificSource Source { get; set; } = null!;

    public Unit Unit { get; set; } = null!;

    public Mission? Mission { get; set; }

    public MarsLocation? Location { get; set; }
}

