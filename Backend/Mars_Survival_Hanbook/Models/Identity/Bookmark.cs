namespace Mars_Survival_Hanbook.Models;

/// <summary>An optional reader's saved article.</summary>
public class Bookmark
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid UserId { get; set; }

    public Guid ArticleId { get; set; }

    public User User { get; set; } = null!;

    public Article Article { get; set; } = null!;
}

