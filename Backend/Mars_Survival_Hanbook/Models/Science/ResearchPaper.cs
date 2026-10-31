using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Research-specific metadata; bibliographic title, authors, date and DOI live on Source.</summary>
public class ResearchPaper : Entity
{
    [MaxLength(200)]
    public required string DocumentId { get; set; }

    public string? Abstract { get; set; }

    [MaxLength(200)]
    public string? ReportNumber { get; set; }

    [MaxLength(2048)]
    public string? PdfUrl { get; set; }

    [MaxLength(1000)]
    public string? ResearchEnvironment { get; set; }

    public int? SampleSize { get; set; }

    [MaxLength(4000)]
    public string? Limitations { get; set; }

    public Guid SourceId { get; set; }

    public ScientificSource Source { get; set; } = null!;
}

