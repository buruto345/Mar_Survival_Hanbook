using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Media metadata with mandatory provenance, credit, usage notes and accessible alternative text.</summary>
public class MediaAsset : Entity
{
    [MaxLength(200)]
    public string? NasaId { get; set; }

    [MaxLength(300)]
    public required string Title { get; set; }

    public MediaType MediaType { get; set; } = MediaType.Image;

    [MaxLength(2048)]
    public required string Url { get; set; }

    [MaxLength(2048)]
    public required string OriginalUrl { get; set; }

    [MaxLength(1000)]
    public required string Credit { get; set; }

    [MaxLength(4000)]
    public required string UsageNote { get; set; }

    [MaxLength(2000)]
    public required string AltText { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public long? FileSizeBytes { get; set; }

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

    public DateTimeOffset? VerifiedAt { get; set; }

    public Guid SourceId { get; set; }

    public Guid? VerifiedById { get; set; }

    public ScientificSource Source { get; set; } = null!;

    public User? VerifiedBy { get; set; }

    public ICollection<ArticleMedia> Articles { get; set; } = new List<ArticleMedia>();
}

