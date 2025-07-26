using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HL7Processor.Application.Interfaces;
using HL7Processor.Infrastructure.Auth;
using Microsoft.IdentityModel.Tokens;

namespace HL7Processor.Infrastructure.Services;

public class TokenServiceAdapter : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenServiceAdapter(JwtSettings settings)
    {
        _settings = settings;
    }

    public string GenerateToken(string username, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}