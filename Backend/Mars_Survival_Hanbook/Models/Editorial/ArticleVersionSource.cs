namespace Mars_Survival_Hanbook.Models;

public class ArticleVersionSource
{
    public Guid ArticleVersionId { get; set; }
    public Guid SourceId { get; set; }
    public Guid SourceRevisionId { get; set; }
    public SourceRevision SourceRevision { get; set; } = null!;
}
