using System.ComponentModel.DataAnnotations;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Immutable, indexed snapshot block. Visible only through the active publication.</summary>
public class PublicationResource
{
    public Guid ArticleVersionId { get; set; }
    [MaxLength(32)] public required string Kind { get; set; }
    public Guid ResourceId { get; set; }
    public required string Payload { get; set; }
    [MaxLength(500)] public required string Name { get; set; }
    [MaxLength(100)] public string? Status { get; set; }
    [MaxLength(100)] public string? Type { get; set; }
    [MaxLength(100)] public string? QuantityKind { get; set; }
    [MaxLength(48)] public string? EvidenceType { get; set; }
    public Guid? MissionId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? FactId { get; set; }
    public DateTimeOffset? Date { get; set; }
    public bool IsPlanned { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
