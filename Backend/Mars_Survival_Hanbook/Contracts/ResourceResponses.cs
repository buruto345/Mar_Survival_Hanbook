namespace Mars_Survival_Hanbook.Contracts;

public record UnitResponse(Guid Id, string Symbol, string QuantityKind);

public class FactResponse : FactRequest
{
    public Guid Id { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ArticleVersionId { get; set; }
    public Guid? ArticleId { get; set; }
    public UnitResponse? Unit { get; set; }
    public Citation? Source { get; set; }
    public RelatedArticle[]? RelatedArticles { get; set; }
}

public class MeasurementResponse : MeasurementRequest
{
    public Guid Id { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ArticleVersionId { get; set; }
    public Guid? ArticleId { get; set; }
    public UnitResponse? Unit { get; set; }
    public Citation? Source { get; set; }
    public RelatedArticle[]? RelatedArticles { get; set; }
}

public class MissionResponse : MissionRequest
{
    public Guid Id { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ArticleVersionId { get; set; }
    public Guid? ArticleId { get; set; }
    
    public Citation? Source { get; set; }
    public RelatedArticle[]? RelatedArticles { get; set; }
}

public class LocationResponse : LocationRequest
{
    public Guid Id { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ArticleVersionId { get; set; }
    public Guid? ArticleId { get; set; }
    
    public Citation? Source { get; set; }
    public RelatedArticle[]? RelatedArticles { get; set; }
}

public class TimelineEventResponse : TimelineEventRequest
{
    public Guid Id { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ArticleVersionId { get; set; }
    public Guid? ArticleId { get; set; }
    
    public Citation? Source { get; set; }
    public RelatedArticle[]? RelatedArticles { get; set; }
}

public class MediaResponse : MediaRequest
{
    public int? DisplayOrder { get; set; }
    public string? Caption { get; set; }
    public Guid Id { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ArticleVersionId { get; set; }
    public Guid? ArticleId { get; set; }
    
    public Citation? Source { get; set; }
    public RelatedArticle[]? RelatedArticles { get; set; }
}

public class MapLayerResponse : MapLayerRequest
{
    public Guid Id { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ArticleVersionId { get; set; }
    public Guid? ArticleId { get; set; }
    
    public Citation? Source { get; set; }
    public RelatedArticle[]? RelatedArticles { get; set; }
}
