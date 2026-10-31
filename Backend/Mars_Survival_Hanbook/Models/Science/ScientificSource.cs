using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>NASA provenance registry. A NASA domain alone does not establish accepted ownership.</summary>
public class ScientificSource : Entity
{
    [MaxLength(500)]
    public required string Title { get; set; }

    [MaxLength(300)]
    public required string NASAOrganization { get; set; }

    [MaxLength(2048)]
    public required string Url { get; set; }

    [MaxLength(2048)]
    public string? CanonicalUrl { get; set; }

    [MaxLength(300)]
    public string? Doi { get; set; }

    [MaxLength(200)]
    public string? ExternalRecordId { get; set; }

    [MaxLength(2000)]
    public string? Authors { get; set; }

    [MaxLength(500)]
    public string? MissionOwnership { get; set; }

    public SourceType SourceType { get; set; } = SourceType.WebPage;

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

    [MaxLength(4000)]
    public string? VerificationNotes { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset RetrievedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? VerifiedAt { get; set; }

    public DateTimeOffset? NextReviewAt { get; set; }

    [MaxLength(128)]
    public string? Checksum { get; set; }

    public Guid? VerifiedById { get; set; }

    public Guid? IngestionJobId { get; set; }

    public User? VerifiedBy { get; set; }

    public IngestionJob? IngestionJob { get; set; }

    public ICollection<ScientificFact> Facts { get; set; } = new List<ScientificFact>();

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();

    public ICollection<Hazard> Hazards { get; set; } = new List<Hazard>();

    public ICollection<Mission> Missions { get; set; } = new List<Mission>();

    public ICollection<MarsLocation> Locations { get; set; } = new List<MarsLocation>();

    public ICollection<ExplorationEvent> ExplorationEvents { get; set; } = new List<ExplorationEvent>();

    public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();

    public ICollection<MapLayer> MapLayers { get; set; } = new List<MapLayer>();

    public ICollection<GlossaryTerm> GlossaryTerms { get; set; } = new List<GlossaryTerm>();

    public ICollection<ArticleSource> Articles { get; set; } = new List<ArticleSource>();

    public ICollection<TechnologySource> Technologies { get; set; } = new List<TechnologySource>();
}

