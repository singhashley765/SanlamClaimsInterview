using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SanlamClaims.API.Auth;

namespace SanlamClaims.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthOptions _options;
    private readonly TokenService _tokenService;

    public AuthController(IOptions<AuthOptions> options, TokenService tokenService)
    {
        _options = options.Value;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("public")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (request.Username != _options.Username || request.Password != _options.Password)
        {
            return Unauthorized();
        }

        return Ok(_tokenService.GenerateToken(request.Username));
    }
}
