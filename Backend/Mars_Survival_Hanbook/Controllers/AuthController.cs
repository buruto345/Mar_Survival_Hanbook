using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace Mars_Survival_Hanbook.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(AuthenticationService auth) : ControllerBase
{
    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) => Ok(await auth.LoginAsync(request, ct));
}
