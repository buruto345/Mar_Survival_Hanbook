namespace Mars_Survival_Hanbook.Models;

/// <summary>Explicit association with a composite key.</summary>
public class ArticleGlossaryTerm
{
    public Guid ArticleId { get; set; }

    public Guid GlossaryTermId { get; set; }

    public Article Article { get; set; } = null!;

    public GlossaryTerm GlossaryTerm { get; set; } = null!;
}

