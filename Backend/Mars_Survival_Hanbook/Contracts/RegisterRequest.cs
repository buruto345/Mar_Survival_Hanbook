using System.ComponentModel.DataAnnotations;

namespace Mars_Survival_Hanbook.Contracts;

public sealed class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]*$", ErrorMessage = "Username must start with a letter or digit and contain only letters, digits, dots, underscores or hyphens.")]
    public string Username { get; set; } = "";

    [Required, StringLength(150)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = "";
}
