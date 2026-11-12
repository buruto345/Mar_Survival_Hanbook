using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

public sealed class FacilityFlow
{
    public Guid ScenarioVersionId { get; init; }
    public Guid TechnologyId { get; init; }
    public Guid ResourceId { get; init; }
    [MaxLength(80)] public required string ParameterKey { get; init; }
    [MaxLength(24)] public required string Phase { get; init; }
    public ResourceDirection Direction { get; init; }
}
