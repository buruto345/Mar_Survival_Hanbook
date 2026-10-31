using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>NASA mission metadata with explicitly dated status verification.</summary>
public class Mission : Entity
{
    [MaxLength(150)]
    public required string NASAIdentifier { get; set; }

    [MaxLength(250)]
    public required string Name { get; set; }

    [MaxLength(200)]
    public required string Agency { get; set; }

    public string? Description { get; set; }

    public MissionType MissionType { get; set; } = MissionType.Other;

    public MissionStatus Status { get; set; } = MissionStatus.Unknown;

    public DateTimeOffset? LaunchDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public DateTimeOffset? StatusVerifiedAt { get; set; }

    public Guid SourceId { get; set; }

    public ScientificSource Source { get; set; } = null!;

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();

    public ICollection<ExplorationEvent> Events { get; set; } = new List<ExplorationEvent>();
}

