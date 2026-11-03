using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace Mars_Survival_Hanbook.Services;

public record JwtSettings(string Issuer, string Audience, string SigningKey, int ExpiryMinutes);
public sealed class AuthenticationService(MarsDbContext db, IPasswordHasher<User> hasher, JwtSettings jwt)
{
    private static readonly User Dummy = new() { Email = "invalid@example.invalid", NormalizedEmail = "", DisplayName = "" };
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(Dummy, Guid.NewGuid().ToString());
    public async Task<object> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == email, ct);
        var result = hasher.VerifyHashedPassword(user ?? Dummy, user?.PasswordHash ?? DummyHash, request.Password);
        if (user == null || result == PasswordVerificationResult.Failed || user.State != UserState.Active || user.ArchivedAt != null)
            throw new ApiException(401, "invalid_credentials", "Email hoặc mật khẩu không hợp lệ.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user, request.Password);
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new { AccessToken = new JwtSecurityTokenHandler().WriteToken(token), TokenType = "Bearer",
            ExpiresIn = jwt.ExpiryMinutes * 60, Profile = new { user.Id, user.DisplayName, user.Role } };
    }
}
