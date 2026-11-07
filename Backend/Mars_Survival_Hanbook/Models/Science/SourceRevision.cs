using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

public class SourceRevision : Entity
{
    public Guid SourceId { get; set; }
    public ScientificSource Source { get; set; } = null!;
    public int RevisionNumber { get; set; }
    [MaxLength(200)] public string? ProviderVersion { get; set; }
    [MaxLength(64)] public required string ContentHash { get; set; }
    [MaxLength(64)] public required string Fingerprint { get; set; }
    [MaxLength(100)] public string NormalizationVersion { get; set; } = "source-v1";
    public required string MetadataSnapshot { get; set; }
    [MaxLength(2048)] public string? RawStoragePath { get; set; }
    [MaxLength(64)] public string? RawChecksum { get; set; }
    public Guid? IngestionJobId { get; set; }
    public DateTimeOffset RetrievedAt { get; set; } = DateTimeOffset.UtcNow;
    public VerificationStatus VerificationStatus { get; set; }
    public Guid? VerifiedById { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    [MaxLength(4000)] public string? VerificationNotes { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    [MaxLength(4000)] public string? RevocationReason { get; set; }
}
