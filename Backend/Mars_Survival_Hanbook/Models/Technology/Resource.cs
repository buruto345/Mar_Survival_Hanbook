using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Water, oxygen, energy, materials or another input/output resource.</summary>
public class Resource : Entity
{
    [MaxLength(150)]
    public required string Name { get; set; }

    [MaxLength(180)]
    public required string Slug { get; set; }

    [MaxLength(100)]
    public required string Category { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    public ICollection<TechnologyResource> Technologies { get; set; } = new List<TechnologyResource>();

    public ICollection<ArticleResource> Articles { get; set; } = new List<ArticleResource>();
}

