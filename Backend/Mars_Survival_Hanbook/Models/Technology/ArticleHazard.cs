namespace Mars_Survival_Hanbook.Models;

/// <summary>Explicit association with a composite key.</summary>
public class ArticleHazard
{
    public Guid ArticleId { get; set; }

    public Guid HazardId { get; set; }

    public Article Article { get; set; } = null!;

    public Hazard Hazard { get; set; } = null!;
}

