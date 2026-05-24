using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Hysteria2Dashboard.Application.Services;

public class AuthService(IAppConfigStore appConfigStore) : IAuthService
{
    public async Task<string> LoginAsync(string password)
    {
        var storedHash = await appConfigStore.GetAdminPasswordHashAsync();

        var isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid password");

        return await GenerateJwtTokenAsync();
    }

    private async Task<string> GenerateJwtTokenAsync()
    {
        var secret = await appConfigStore.GetJwtSecretAsync();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}