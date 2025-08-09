using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Configs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class JwtService(IOptions<JwtConfig> config) : IJwtService
{
    public string GenerateToken(ApplicationUser user, IEnumerable<string> roleNames)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secret = Encoding.UTF8.GetBytes(config.Value.Secret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Nickname, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        };

        claims.AddRange(roleNames.Select(roleName => new Claim(ClaimTypes.Role, roleName)));

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = config.Value.Issuer,
            Audience = config.Value.Audience,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return jwt;
    }
}