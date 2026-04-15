using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MGSPlus.Api.Models;

namespace MGSPlus.Api.Services;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>Generate a signed JWT access token for the given user.</summary>
    public string GenerateToken(User user)
    {
        var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim("firstName",                   user.FirstName),
            new Claim("lastName",                    user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer:            _config["Jwt:Issuer"],
            audience:          _config["Jwt:Audience"],
            claims:            claims,
            expires:           DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate a cryptographically random refresh token (raw) and its SHA-256 hash.
    /// Returns (rawToken, hashedToken, expiresAt).
    /// Store only the hash; return the raw token to the client.
    /// </summary>
    public (string raw, string hashed, DateTime expiresAt) GenerateRefreshToken()
    {
        var raw     = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashed  = HashToken(raw);
        var expires = DateTime.UtcNow.AddDays(7);
        return (raw, hashed, expires);
    }

    /// <summary>SHA-256 hash a token string for safe storage.</summary>
    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Extract the integer user ID from the ClaimsPrincipal.</summary>
    public int? GetUserIdFromToken(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(sub, out var id) ? id : null;
    }
}
