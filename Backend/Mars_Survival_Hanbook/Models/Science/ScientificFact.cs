using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>A reusable scientific value preserving its original value, unit, context and source.</summary>
public class ScientificFact : Entity
{
    [MaxLength(300)]
    public required string Name { get; set; }

    [Precision(28, 12)]
    public decimal Value { get; set; }

    [Precision(28, 12)]
    public decimal? UpperValue { get; set; }

    public MeasurementKind MeasurementKind { get; set; } = MeasurementKind.Unspecified;

    [Precision(28, 12)]
    public decimal OriginalValue { get; set; }

    [MaxLength(64)]
    public required string OriginalUnit { get; set; }

    [MaxLength(4000)]
    public required string MeasurementContext { get; set; }

    [MaxLength(4000)]
    public string? Assumptions { get; set; }

    [MaxLength(2000)]
    public string? Uncertainty { get; set; }

    public EvidenceType EvidenceType { get; set; } = EvidenceType.Unspecified;

    public TestEnvironment TestEnvironment { get; set; } = TestEnvironment.Unspecified;

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

    public DateTimeOffset? VerifiedAt { get; set; }

    public DateTimeOffset? ObservedAt { get; set; }

    public Guid SourceId { get; set; }

    public Guid UnitId { get; set; }

    public Guid? VerifiedById { get; set; }

    public ScientificSource Source { get; set; } = null!;

    public Unit Unit { get; set; } = null!;

    public User? VerifiedBy { get; set; }

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();

    public ICollection<ArticleFact> Articles { get; set; } = new List<ArticleFact>();

    public ICollection<TechnologyFact> Technologies { get; set; } = new List<TechnologyFact>();
}

