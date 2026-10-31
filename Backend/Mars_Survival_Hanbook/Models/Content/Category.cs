using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>A topic within a module, optionally nested under a category in the same module.</summary>
public class Category : Entity
{
    [MaxLength(150)]
    public required string Name { get; set; }

    [MaxLength(180)]
    public required string Slug { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public Guid ModuleId { get; set; }

    public Guid? ParentId { get; set; }

    public Module Module { get; set; } = null!;

    public Category? Parent { get; set; }

    public ICollection<Category> Children { get; set; } = new List<Category>();

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}

