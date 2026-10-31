using System.ComponentModel.DataAnnotations;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Explicit association with a composite key.</summary>
public class ArticleFact
{
    public int DisplayOrder { get; set; }

    [MaxLength(2000)]
    public string? ContextNote { get; set; }

    public Guid ArticleId { get; set; }

    public Guid FactId { get; set; }

    public Article Article { get; set; } = null!;

    public ScientificFact Fact { get; set; } = null!;
}

