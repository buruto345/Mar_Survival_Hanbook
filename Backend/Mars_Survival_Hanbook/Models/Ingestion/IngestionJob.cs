using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Import execution and raw staging metadata; successful ingestion does not publish content.</summary>
public class IngestionJob : Entity
{
    [MaxLength(2048)]
    public required string Endpoint { get; set; }

    public IngestionStatus Status { get; set; } = IngestionStatus.Queued;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? RetrievedAt { get; set; }

    [MaxLength(128)]
    public string? Checksum { get; set; }

    [MaxLength(100)]
    public required string ParserVersion { get; set; }

    public string? RawPayload { get; set; }

    [MaxLength(2048)]
    public string? RawStoragePath { get; set; }

    [MaxLength(4000)]
    public string? ErrorMessage { get; set; }

    public int AttemptCount { get; set; }

    public int RecordsFetched { get; set; }

    public int RecordsAccepted { get; set; }

    public int RecordsRejected { get; set; }

    public Guid ConnectorId { get; set; }

    public Guid? RequestedById { get; set; }

    public NasaConnector Connector { get; set; } = null!;

    public User? RequestedBy { get; set; }

    public ICollection<ScientificSource> Sources { get; set; } = new List<ScientificSource>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

