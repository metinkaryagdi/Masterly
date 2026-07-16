using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TrainingPlatform.Application.Abstractions.Security;
using TrainingPlatform.Domain.Identity;

namespace TrainingPlatform.Infrastructure.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, IOptions<AdminAccessOptions> adminAccess) : IJwtTokenGenerator
{
    public string Generate(User user)
    {
        var jwtOptions = options.Value;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.DisplayName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        if (adminAccess.Value.IsAdmin(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
