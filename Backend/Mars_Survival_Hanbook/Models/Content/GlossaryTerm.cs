using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>A sourced scientific glossary definition.</summary>
public class GlossaryTerm : Entity
{
    [MaxLength(200)]
    public required string Term { get; set; }

    [MaxLength(200)]
    public required string Slug { get; set; }

    [MaxLength(4000)]
    public required string Definition { get; set; }

    [MaxLength(10)]
    public required string LanguageCode { get; set; }

    public Guid SourceId { get; set; }

    public ScientificSource Source { get; set; } = null!;

    public ICollection<GlossaryAlias> Aliases { get; set; } = new List<GlossaryAlias>();

    public ICollection<ArticleGlossaryTerm> Articles { get; set; } = new List<ArticleGlossaryTerm>();
}

