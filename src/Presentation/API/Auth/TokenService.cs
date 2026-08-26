using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SanlamClaims.API.Auth;

public class TokenService
{
    private readonly AuthOptions _options;

    public TokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public LoginResponse GenerateToken(string username)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.JwtExpiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _options.JwtIssuer,
            _options.JwtAudience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
