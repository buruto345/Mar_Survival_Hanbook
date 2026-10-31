using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Resource input/output; direction is part of the composite key.</summary>
public class TechnologyResource
{
    public ResourceDirection Direction { get; set; } = ResourceDirection.Input;

    [MaxLength(2000)]
    public string? ProcessNote { get; set; }

    public Guid TechnologyId { get; set; }

    public Guid ResourceId { get; set; }

    public Technology Technology { get; set; } = null!;

    public Resource Resource { get; set; } = null!;
}

