namespace Mars_Survival_Hanbook.Models;

/// <summary>Explicit association with a composite key.</summary>
public class ArticleTechnology
{
    public Guid ArticleId { get; set; }

    public Guid TechnologyId { get; set; }

    public Article Article { get; set; } = null!;

    public Technology Technology { get; set; } = null!;
}

