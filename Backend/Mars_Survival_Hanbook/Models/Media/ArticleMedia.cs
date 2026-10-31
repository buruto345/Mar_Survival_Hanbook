using System.ComponentModel.DataAnnotations;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Explicit association with a composite key.</summary>
public class ArticleMedia
{
    public int DisplayOrder { get; set; }

    [MaxLength(2000)]
    public string? Caption { get; set; }

    public Guid ArticleId { get; set; }

    public Guid MediaId { get; set; }

    public Article Article { get; set; } = null!;

    public MediaAsset Media { get; set; } = null!;
}

