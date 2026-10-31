using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Reusable article tag.</summary>
public class Tag : Entity
{
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(120)]
    public required string Slug { get; set; }

    public ICollection<ArticleTag> Articles { get; set; } = new List<ArticleTag>();
}

