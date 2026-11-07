using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
namespace Mars_Survival_Hanbook.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(AuthenticationService auth, CurrentActor actor) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
        => CreatedAtAction(nameof(Me), await auth.RegisterAsync(request, ct));

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) => Ok(await auth.LoginAsync(request, ct));

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me(CancellationToken ct) => Ok(await auth.GetProfileAsync(actor.Id, ct));
}
