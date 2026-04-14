using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MGSPlus.Api.Models;
using MGSPlus.Api.Services;
using MGSPlus.Tests.Helpers;

namespace MGSPlus.Tests.Services;

/// <summary>
/// Unit tests for JwtService.
/// No DB or HTTP involved — purely exercises token generation / claim extraction.
/// </summary>
public class JwtServiceTests
{
    private readonly JwtService _sut = DbHelper.CreateJwtService();

    private static User MakeUser(int id = 1, string role = "Patient") => new()
    {
        Id        = id,
        Email     = $"user{id}@example.com",
        FirstName = "Test",
        LastName  = "User",
        Role      = role,
        PasswordHash = "hash"
    };

    // ── GenerateToken ─────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateToken(MakeUser());
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ProducesValidJwt()
    {
        var token = _sut.GenerateToken(MakeUser());
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("MGSPlus", jwt.Issuer);
        Assert.Contains("MGSPlusApp", jwt.Audiences);
    }

    [Fact]
    public void GenerateToken_EmbedsSub_Email_Role()
    {
        var user = MakeUser(42, "Doctor");
        var token = _sut.GenerateToken(user);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Doctor");
    }

    [Fact]
    public void GenerateToken_EmbedsFirstName_LastName()
    {
        var user = new User { Id = 1, Email = "a@b.com", FirstName = "Alice", LastName = "Wonder", Role = "Patient", PasswordHash = "x" };
        var token = _sut.GenerateToken(user);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "firstName" && c.Value == "Alice");
        Assert.Contains(jwt.Claims, c => c.Type == "lastName"  && c.Value == "Wonder");
    }

    [Fact]
    public void GenerateToken_ExpiresInFuture()
    {
        var token = _sut.GenerateToken(MakeUser());
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_Signature_ValidWithCorrectKey()
    {
        var config = DbHelper.JwtConfig();
        var jwt    = _sut.GenerateToken(MakeUser());

        var handler   = new JwtSecurityTokenHandler();
        var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
        var validated = handler.ValidateToken(jwt, new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = config["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = config["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = key,
            ClockSkew                = TimeSpan.Zero
        }, out _);

        Assert.NotNull(validated);
    }

    [Fact]
    public void GenerateToken_ThrowsWhenSecretMissing()
    {
        var badConfig = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = null })
            .Build();

        var svc = new JwtService(badConfig);
        Assert.Throws<InvalidOperationException>(() => svc.GenerateToken(MakeUser()));
    }

    [Fact]
    public void GenerateToken_IncludesJtiClaim()
    {
        var t1 = new JwtSecurityTokenHandler().ReadJwtToken(_sut.GenerateToken(MakeUser()));
        var t2 = new JwtSecurityTokenHandler().ReadJwtToken(_sut.GenerateToken(MakeUser()));

        var jti1 = t1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = t2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Assert.NotEqual(jti1, jti2);   // each token has unique JTI
    }

    // ── GetUserIdFromToken ────────────────────────────────────────────────────

    [Fact]
    public void GetUserIdFromToken_ReturnsCorrectId()
    {
        var user  = MakeUser(99);
        var token = _sut.GenerateToken(user);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var principal = BuildPrincipal(jwt);
        var id = _sut.GetUserIdFromToken(principal);

        Assert.Equal(99, id);
    }

    [Fact]
    public void GetUserIdFromToken_ReturnsNull_WhenNoSubClaim()
    {
        var identity  = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        Assert.Null(_sut.GetUserIdFromToken(principal));
    }

    [Fact]
    public void GetUserIdFromToken_ReturnsNull_WhenSubIsNotInteger()
    {
        var identity  = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-number") });
        var principal = new ClaimsPrincipal(identity);
        Assert.Null(_sut.GetUserIdFromToken(principal));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ClaimsPrincipal BuildPrincipal(JwtSecurityToken jwt)
    {
        var identity = new ClaimsIdentity(jwt.Claims);
        return new ClaimsPrincipal(identity);
    }
}
