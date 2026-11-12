using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

public sealed class LivingScenarioVersion : Entity
{
    [MaxLength(80)] public required string ScenarioKey { get; init; }
    public int Version { get; init; }
    [MaxLength(200)] public required string Title { get; init; }
    public required string Snapshot { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public ICollection<ScenarioParameter> Parameters { get; } = new List<ScenarioParameter>();
    public ICollection<ScenarioResource> Resources { get; } = new List<ScenarioResource>();
    public ICollection<FacilityDefinition> Facilities { get; } = new List<FacilityDefinition>();
}
public sealed class ScenarioParameter
{
    public Guid ScenarioVersionId { get; init; }
    [MaxLength(80)] public required string Key { get; init; }
    public decimal OriginalValue { get; init; }
    public decimal NormalizedValue { get; init; }
    [MaxLength(80)] public required string OriginalBasis { get; init; }
    [MaxLength(80)] public required string NormalizedBasis { get; init; }
    [MaxLength(24)] public required string Unit { get; init; }
    public Guid? SourceRevisionId { get; init; }
    public SourceRevision? SourceRevision { get; init; }
}
public sealed class ScenarioResource
{
    public Guid ScenarioVersionId { get; init; }
    public required Resource Resource { get; init; }
    public Guid ResourceId { get; init; }
    public Guid UnitId { get; init; }
    public required Unit Unit { get; init; }
    public decimal InitialQuantity { get; init; }
    public decimal Capacity { get; init; }
}
public sealed class FacilityDefinition
{
    public Guid ScenarioVersionId { get; init; }
    public required Technology Technology { get; init; }
    public Guid TechnologyId { get; init; }
    public int DefaultCount { get; init; }
    public int MaxCount { get; init; }
}
public sealed class LivingEnglishReview
{
    public Guid ArticleVersionId { get; init; }
    public required ArticleVersion ArticleVersion { get; init; }
    [MaxLength(64)] public required string SnapshotHash { get; init; }
    public Guid ReviewerId { get; init; }
    public DateTimeOffset ReviewedAt { get; init; }
}
