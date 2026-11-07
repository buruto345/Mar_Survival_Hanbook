using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

public class IngestionItem : Entity
{
    public Guid JobId { get; set; }
    [MaxLength(450)] public required string ItemKey { get; set; }
    public SourceProvider Provider { get; set; }
    [MaxLength(450)] public string? IdentityKey { get; set; }
    public string? RawPayload { get; set; }
    [MaxLength(2048)] public string? RawStoragePath { get; set; }
    [MaxLength(64)] public string? RawChecksum { get; set; }
    public string? NormalizedPayload { get; set; }
    [MaxLength(100)] public string NormalizationVersion { get; set; } = "source-v1";
    public Guid? SourceId { get; set; }
    public Guid? SourceRevisionId { get; set; }
    public IngestionItemStatus Status { get; set; }
    [MaxLength(100)] public string? ErrorCode { get; set; }
    [MaxLength(2000)] public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
