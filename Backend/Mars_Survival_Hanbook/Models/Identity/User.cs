using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mars_Survival_Hanbook.Models.Common;
using Mars_Survival_Hanbook.Models.Enums;

namespace Mars_Survival_Hanbook.Models;

/// <summary>Application account. PasswordHash is never a plaintext password and is excluded from JSON.</summary>
public class User : Entity
{
    [MaxLength(50)]
    public required string Username { get; set; }

    [JsonIgnore, Required, MaxLength(50)]
    public string NormalizedUsername { get; set; } = null!;

    [MaxLength(254)]
    public string? Email { get; set; }

    [JsonIgnore, MaxLength(254)]
    public string? NormalizedEmail { get; set; }

    [MaxLength(150)]
    public string? FullName { get; set; }

    [JsonIgnore]
    [Required]
    [MaxLength(1000)]
    public string PasswordHash { get; set; } = null!;

    public UserRole Role { get; set; } = UserRole.Viewer;

    public UserState State { get; set; } = UserState.Active;

    [MaxLength(10)]
    public string? PreferredLanguage { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<Article> AuthoredArticles { get; set; } = new List<Article>();

    public ICollection<ReviewTask> AssignedReviews { get; set; } = new List<ReviewTask>();

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}

