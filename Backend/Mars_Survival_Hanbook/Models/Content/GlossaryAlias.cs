using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>An alternate name, abbreviation or search synonym for a glossary term.</summary>
public class GlossaryAlias : Entity
{
    [MaxLength(200)]
    public required string Alias { get; set; }

    public Guid GlossaryTermId { get; set; }

    public GlossaryTerm GlossaryTerm { get; set; } = null!;
}

