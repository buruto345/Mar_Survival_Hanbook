using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
namespace Mars_Survival_Hanbook.Services;

public record JwtSettings(string Issuer, string Audience, string SigningKey, int ExpiryMinutes);
public sealed class AuthenticationService(MarsDbContext db, IPasswordHasher<User> hasher, JwtSettings jwt)
{
    private static readonly User Dummy = new() { Username = "invalid", NormalizedUsername = "INVALID",
        Email = "invalid@example.invalid", NormalizedEmail = "", FullName = "" };
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(Dummy, Guid.NewGuid().ToString());
    private static UserProfile Profile(User user) => new(user.Id, user.Username, user.FullName, user.Email, user.Role);

    public Task<UserProfile> RegisterAsync(RegisterRequest request, CancellationToken ct) =>
        CreateAccountAsync(request, UserRole.Viewer, ct);

    // Used only by the local operator CLI. The public registration DTO has no role field.
    public Task<UserProfile> ProvisionAsync(RegisterRequest request, UserRole role, CancellationToken ct) =>
        CreateAccountAsync(request, role, ct);

    private async Task<UserProfile> CreateAccountAsync(RegisterRequest request, UserRole role, CancellationToken ct)
    {
        if (!Enum.IsDefined(role)) throw ApiException.Invalid("Role must be Admin or Viewer.");
        if (role == UserRole.Admin)
        {
            // Operator-only provisioning allows minimal credentials; keep storage/login limits.
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 50 ||
                string.IsNullOrWhiteSpace(request.Password) || request.Password.Length > 1024 ||
                request.Email?.Length > 254 || request.FullName?.Length > 150)
                throw ApiException.Invalid("Admin requires a username (1-50 characters) and password (1-1024 characters).");
        }
        else Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var normalizedEmail = email?.ToUpperInvariant();
        var username = request.Username.Trim();
        var normalizedUsername = username.ToUpperInvariant();
        if (await db.Users.AnyAsync(x => (normalizedEmail != null && x.NormalizedEmail == normalizedEmail) || x.NormalizedUsername == normalizedUsername, ct))
            throw ApiException.Conflict("Username or email is already registered.");
        var user = new User { Username = username, NormalizedUsername = normalizedUsername,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(), Email = email, NormalizedEmail = normalizedEmail,
            Role = role, State = UserState.Active };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        Audit.Add(db, user.Id, nameof(User), user.Id, AuditAction.Created, "Created account with role " + role);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        { throw ApiException.Conflict("Username or email is already registered."); }
        return Profile(user);
    }

    public async Task<UserProfile> GetProfileAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.State == UserState.Active && x.ArchivedAt == null, ct)
            ?? throw new ApiException(401, "invalid_credentials", "Account is not active.");
        return Profile(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var useEmail = !string.IsNullOrWhiteSpace(request.Email);
        var identifier = (useEmail ? request.Email! : request.Username!).Trim().ToUpperInvariant();
        var user = useEmail
            ? await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == identifier, ct)
            : await db.Users.SingleOrDefaultAsync(x => x.NormalizedUsername == identifier, ct);
        // Disabled service identities have no interactive password hash.
        var active = user is { State: UserState.Active, ArchivedAt: null };
        var result = PasswordVerificationResult.Failed;
        try { result = hasher.VerifyHashedPassword(active ? user! : Dummy, active ? user!.PasswordHash : DummyHash, request.Password); }
        catch (FormatException) { /* Invalid stored hashes must not turn a rejected login into HTTP 500. */ }
        if (user == null || result == PasswordVerificationResult.Failed || user.State != UserState.Active || user.ArchivedAt != null)
            throw new ApiException(401, "invalid_credentials", "Email, tên đăng nhập hoặc mật khẩu không hợp lệ.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user, request.Password);
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", jwt.ExpiryMinutes * 60, Profile(user));
    }
}
