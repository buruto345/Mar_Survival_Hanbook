using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Top-level handbook area; the initial taxonomy contains five modules.</summary>
public class Module : Entity
{
    [MaxLength(150)]
    public required string Name { get; set; }

    [MaxLength(180)]
    public required string Slug { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

