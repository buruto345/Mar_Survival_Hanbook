using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Server-side NASA connector configuration; store only secret references, never credentials.</summary>
public class NasaConnector : Entity
{
    [MaxLength(100)]
    public required string Key { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    public ConnectorType ConnectorType { get; set; } = ConnectorType.Images;

    [MaxLength(2048)]
    public required string BaseUrl { get; set; }

    [MaxLength(300)]
    public string? AuthenticationSecretReference { get; set; }

    [MaxLength(4000)]
    public string? AccessPolicy { get; set; }

    public string? ConfigurationJson { get; set; }

    public bool IsEnabled { get; set; }

    public int? RateLimitPerMinute { get; set; }

    public DateTimeOffset? LastSyncedAt { get; set; }

    public ICollection<IngestionJob> Jobs { get; set; } = new List<IngestionJob>();
}

