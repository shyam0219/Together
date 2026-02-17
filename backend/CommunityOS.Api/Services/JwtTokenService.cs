using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommunityOS.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CommunityOS.Api.Services;

public interface IJwtTokenService
{
    string CreateToken(User user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(User user)
    {
        var key = _config["Auth:JwtSigningKey"] ?? throw new InvalidOperationException("Auth:JwtSigningKey missing");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var expiresMinutes = int.TryParse(_config["Auth:JwtExpiryMinutes"], out var m) ? m : 60 * 24;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("tenant_id", user.TenantId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Auth:JwtIssuer"],
            audience: _config["Auth:JwtAudience"],
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(expiresMinutes).UtcDateTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
