using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Contracts;

public abstract class ProvenancedRequest
{
    [JsonRequired] public Guid SourceRevisionId { get; set; }
}

public class FactRequest : ProvenancedRequest
{
    [MaxLength(300)] [Required] public string Name { get; set; } = "";
    [JsonRequired] public decimal Value { get; set; }
    public decimal? UpperValue { get; set; }
    [JsonRequired] public Guid UnitId { get; set; }
    [JsonRequired] public decimal OriginalValue { get; set; }
    [MaxLength(64)] [Required] public string OriginalUnit { get; set; } = "";
    [JsonRequired] public MeasurementKind MeasurementKind { get; set; }
    [MaxLength(4000)] [Required] public string Context { get; set; } = "";
    [MaxLength(4000)] public string? Assumptions { get; set; }
    [MaxLength(2000)] public string? Uncertainty { get; set; }
    [JsonRequired] public EvidenceType EvidenceType { get; set; }
    [JsonRequired] public TestEnvironment TestEnvironment { get; set; }
    public DateTimeOffset? ObservedAt { get; set; }
}

public class MeasurementRequest : ProvenancedRequest
{
    [JsonRequired] public Guid FactId { get; set; }
    [JsonRequired] public decimal Value { get; set; }
    public decimal? UpperValue { get; set; }
    [JsonRequired] public Guid UnitId { get; set; }
    [JsonRequired] public decimal OriginalValue { get; set; }
    [MaxLength(64)] [Required] public string OriginalUnit { get; set; } = "";
    [JsonRequired] public MeasurementKind MeasurementKind { get; set; }
    [JsonRequired] public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset? ObservationEndAt { get; set; }
    [MaxLength(200)] [Required] public string Instrument { get; set; } = "";
    [MaxLength(4000)] [Required] public string Context { get; set; } = "";
    [MaxLength(2000)] public string? Uncertainty { get; set; }
    [JsonRequired] public EvidenceType EvidenceType { get; set; }
    [JsonRequired] public TestEnvironment TestEnvironment { get; set; }
    public Guid? MissionId { get; set; }
    public Guid? LocationId { get; set; }
    [MaxLength(100)] [Required] public string QualityFlag { get; set; } = "";
    [MaxLength(2000)] public string? QualityNotes { get; set; }
    [MaxLength(450)] public string? ExternalObservationKey { get; set; }
}

public class MissionRequest : ProvenancedRequest
{
    [MaxLength(150)] [Required] public string NasaIdentifier { get; set; } = "";
    [MaxLength(250)] [Required] public string Name { get; set; } = "";
    [MaxLength(200)] [Required] public string Agency { get; set; } = "";
    [MaxLength(10000)] public string? Description { get; set; }
    [JsonRequired] public MissionType MissionType { get; set; }
    [JsonRequired] public MissionStatus Status { get; set; }
    public DateTimeOffset? LaunchDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset? StatusVerifiedAt { get; set; }
}

public class LocationRequest : ProvenancedRequest
{
    [MaxLength(200)] [Required] public string Name { get; set; } = "";
    [MaxLength(200)] [Required] public string Slug { get; set; } = "";
    [JsonRequired] public decimal Latitude { get; set; }
    [JsonRequired] public decimal Longitude { get; set; }
    [MaxLength(150)] [Required] public string CoordinateSystem { get; set; } = "";
    [MaxLength(4000)] public string? Description { get; set; }
}

public class TimelineEventRequest : ProvenancedRequest
{
    [MaxLength(300)] [Required] public string Title { get; set; } = "";
    [MaxLength(4000)] [Required] public string Description { get; set; } = "";
    [JsonRequired] public DateTimeOffset EventDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    [JsonRequired] public bool IsPlanned { get; set; }
    [MaxLength(200)] [Required] public string Agency { get; set; } = "";
    public Guid? MissionId { get; set; }
}

public class MediaRequest : ProvenancedRequest
{
    [MaxLength(200)] public string? NasaId { get; set; }
    [MaxLength(300)] [Required] public string Title { get; set; } = "";
    [JsonRequired] public MediaType MediaType { get; set; }
    [MaxLength(2048)] [Required] public string Url { get; set; } = "";
    [MaxLength(2048)] [Required] public string OriginalUrl { get; set; } = "";
    [MaxLength(1000)] [Required] public string Credit { get; set; } = "";
    [MaxLength(4000)] [Required] public string UsageNote { get; set; } = "";
    [MaxLength(2000)] [Required] public string AltText { get; set; } = "";
    [MaxLength(100)] public string? MimeType { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSizeBytes { get; set; }
    [MaxLength(64)] public string? FileChecksum { get; set; }
}

public class MapLayerRequest : ProvenancedRequest
{
    [MaxLength(200)] [Required] public string Name { get; set; } = "";
    [MaxLength(2000)] public string? Description { get; set; }
    [MaxLength(2048)] [Required] public string ServiceUrl { get; set; } = "";
    [MaxLength(250)] [Required] public string LayerIdentifier { get; set; } = "";
    [MaxLength(200)] [Required] public string TileMatrixSet { get; set; } = "";
    [MaxLength(100)] [Required] public string Format { get; set; } = "";
    [MaxLength(1000)] [Required] public string Attribution { get; set; } = "";
    [MaxLength(200)] [Required] public string Style { get; set; } = "";
    [MaxLength(2048)] [Required] public string ResourceUrlTemplate { get; set; } = "";
    [MaxLength(1000)] [Required] public string BoundingBoxJson { get; set; } = "";
    [MaxLength(150)] [Required] public string CoordinateSystem { get; set; } = "";
    public Guid? LocationId { get; set; }
}
