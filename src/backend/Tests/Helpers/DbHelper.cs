using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MGSPlus.Api.Data;
using MGSPlus.Api.Services;

namespace MGSPlus.Tests.Helpers;

/// <summary>
/// Shared helpers: in-memory DB factory and test JWT config.
/// </summary>
public static class DbHelper
{
    /// <summary>Fresh isolated in-memory DB for each test.</summary>
    public static ApplicationDbContext CreateInMemoryDb(string? dbName = null)
    {
        var name = dbName ?? Guid.NewGuid().ToString();
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(opts);
    }

    /// <summary>IConfiguration with JWT settings suitable for tests.</summary>
    public static IConfiguration JwtConfig(
        string secret = "super-secret-key-for-tests-min32chars!",
        string issuer = "MGSPlus",
        string audience = "MGSPlusApp",
        string expiresMinutes = "60") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]         = secret,
                ["Jwt:Issuer"]         = issuer,
                ["Jwt:Audience"]       = audience,
                ["Jwt:ExpiresMinutes"] = expiresMinutes,
            })
            .Build();

    /// <summary>Pre-configured JwtService backed by test config.</summary>
    public static JwtService CreateJwtService() => new(JwtConfig());
}
