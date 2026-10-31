using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>An ordered section with an article-local anchor for the table of contents.</summary>
public class ArticleSection : Entity
{
    [MaxLength(300)]
    public required string Heading { get; set; }

    [MaxLength(180)]
    public required string Anchor { get; set; }

    public required string Body { get; set; }

    public int DisplayOrder { get; set; }

    public Guid ArticleId { get; set; }

    public Article Article { get; set; } = null!;
}

