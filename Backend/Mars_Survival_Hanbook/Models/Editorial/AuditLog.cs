using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Append-only operational audit entry; application code must redact secrets and sensitive values.</summary>
public class AuditLog : Entity
{
    public AuditAction Action { get; set; } = AuditAction.Created;

    [MaxLength(150)]
    public required string EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(150)]
    public string? RequestId { get; set; }

    [MaxLength(4000)]
    public required string ChangeSummary { get; set; }

    public string? MetadataJson { get; set; }

    public Guid? ActorId { get; set; }

    public Guid? IngestionJobId { get; set; }

    public Guid? SourceId { get; set; }

    public User? Actor { get; set; }

    public IngestionJob? IngestionJob { get; set; }

    public ScientificSource? Source { get; set; }
}

