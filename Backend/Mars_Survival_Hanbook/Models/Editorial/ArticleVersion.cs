using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;

namespace Mars_Survival_Hanbook.Models;

/// <summary>An immutable snapshot to be appended by the publishing or editing service.</summary>
public class ArticleVersion : Entity
{
    public int Version { get; set; }

    public required string Snapshot { get; set; }

    [MaxLength(2000)]
    public string? ChangeSummary { get; set; }

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid ArticleId { get; set; }

    public Guid ChangedById { get; set; }

    public Article Article { get; set; } = null!;

    public User ChangedBy { get; set; } = null!;

    public ICollection<ReviewTask> Reviews { get; set; } = new List<ReviewTask>();
}

