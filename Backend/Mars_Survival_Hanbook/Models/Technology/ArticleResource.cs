namespace Mars_Survival_Hanbook.Models;

/// <summary>Explicit association with a composite key.</summary>
public class ArticleResource
{
    public Guid ArticleId { get; set; }

    public Guid ResourceId { get; set; }

    public Article Article { get; set; } = null!;

    public Resource Resource { get; set; } = null!;
}

