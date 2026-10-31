namespace Mars_Survival_Hanbook.Models.Common;

/// <summary>Stable identity, UTC timestamps and SQL Server optimistic concurrency token.</summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

