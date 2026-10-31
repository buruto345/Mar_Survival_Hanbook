using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>A sourced exploration timeline event, distinguishing plans from historical events.</summary>
public class ExplorationEvent : Entity
{
    [MaxLength(300)]
    public required string Title { get; set; }

    [MaxLength(4000)]
    public required string Description { get; set; }

    public DateTimeOffset EventDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public bool IsPlanned { get; set; }

    [MaxLength(200)]
    public required string Agency { get; set; }

    public Guid SourceId { get; set; }

    public Guid? MissionId { get; set; }

    public ScientificSource Source { get; set; } = null!;

    public Mission? Mission { get; set; }
}

