using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>A directed article relation; self-relations are forbidden.</summary>
public class ArticleRelation
{
    public ArticleRelationType RelationType { get; set; } = ArticleRelationType.Related;

    [MaxLength(2000)]
    public string? Note { get; set; }

    public Guid ArticleId { get; set; }

    public Guid RelatedArticleId { get; set; }

    public Article Article { get; set; } = null!;

    public Article RelatedArticle { get; set; } = null!;
}

