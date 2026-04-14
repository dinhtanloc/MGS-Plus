using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MGSPlus.Api.Controllers;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Models;
using MGSPlus.Tests.Helpers;

namespace MGSPlus.Tests.Controllers;

/// <summary>
/// Unit tests for AuthController.
/// Uses EF Core InMemory — no real SQL Server needed.
/// JwtService is the real implementation (tested separately in JwtServiceTests).
/// IConfiguration is provided via in-memory dictionary.
/// </summary>
public class AuthControllerTests
{
    // ── factories ─────────────────────────────────────────────────────────────

    private static AuthController BuildController(out ApplicationDbContext db)
    {
        db     = DbHelper.CreateInMemoryDb();
        var jwt    = DbHelper.CreateJwtService();
        var config = DbHelper.JwtConfig();
        var ctrl   = new AuthController(db, jwt, config);
        // Provide an empty user context (unauthenticated)
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return ctrl;
    }

    private static AuthController BuildControllerWithUser(int userId, out ApplicationDbContext db)
    {
        db     = DbHelper.CreateInMemoryDb();
        var jwt    = DbHelper.CreateJwtService();
        var config = DbHelper.JwtConfig();
        var ctrl   = new AuthController(db, jwt, config);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Patient")
        };
        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return ctrl;
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_CreatesUser_Returns201()
    {
        var ctrl = BuildController(out var db);
        var req  = new RegisterRequest("alice@example.com", "Password1!", "Alice", "Smith", "0909000001");

        var result = await ctrl.Register(req) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);

        var body = result.Value as AuthResponse;
        Assert.NotNull(body);
        Assert.NotEmpty(body!.Token);
        Assert.Equal("alice@example.com", body.User.Email);
        Assert.Equal("Patient", body.User.Role);
    }

    [Fact]
    public async Task Register_CreatesUserProfile_InDb()
    {
        var ctrl = BuildController(out var db);
        await ctrl.Register(new RegisterRequest("bob@example.com", "Pass1234!", "Bob", "Lee", null));

        var user    = db.Users.First(u => u.Email == "bob@example.com");
        var profile = db.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);
        Assert.NotNull(profile);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var ctrl = BuildController(out _);
        var req  = new RegisterRequest("dup@example.com", "Pass1234!", "A", "B", null);

        await ctrl.Register(req);
        var second = await ctrl.Register(req) as BadRequestObjectResult;

        Assert.NotNull(second);
        Assert.Equal(400, second!.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordIsHashed_NotStoredPlainText()
    {
        var ctrl = BuildController(out var db);
        await ctrl.Register(new RegisterRequest("hash@example.com", "Secret99!", "H", "U", null));

        var user = db.Users.First(u => u.Email == "hash@example.com");
        Assert.NotEqual("Secret99!", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Secret99!", user.PasswordHash));
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var ctrl = BuildController(out _);
        await ctrl.Register(new RegisterRequest("login@example.com", "MyPass1!", "L", "U", null));

        var result = await ctrl.Login(new LoginRequest("login@example.com", "MyPass1!")) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
        var body = result.Value as AuthResponse;
        Assert.NotEmpty(body!.Token);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var ctrl = BuildController(out _);
        await ctrl.Register(new RegisterRequest("wrong@example.com", "Correct1!", "W", "U", null));

        var result = await ctrl.Login(new LoginRequest("wrong@example.com", "WrongPass!")) as UnauthorizedObjectResult;

        Assert.NotNull(result);
        Assert.Equal(401, result!.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var ctrl = BuildController(out _);
        var result = await ctrl.Login(new LoginRequest("nobody@example.com", "Pass1!")) as UnauthorizedObjectResult;
        Assert.Equal(401, result!.StatusCode);
    }

    [Fact]
    public async Task Login_InactiveUser_Returns401()
    {
        var ctrl = BuildController(out var db);
        await ctrl.Register(new RegisterRequest("inactive@example.com", "Pass1234!", "I", "U", null));

        var user = db.Users.First(u => u.Email == "inactive@example.com");
        user.IsActive = false;
        await db.SaveChangesAsync();

        var result = await ctrl.Login(new LoginRequest("inactive@example.com", "Pass1234!")) as UnauthorizedObjectResult;
        Assert.Equal(401, result!.StatusCode);
    }

    // ── Me ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_AuthenticatedUser_ReturnsUserDto()
    {
        var db  = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var config = DbHelper.JwtConfig();

        var user = new User { Email = "me@example.com", FirstName = "Me", LastName = "U", Role = "Patient", PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctrl = new AuthController(db, jwt, config);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Test"))
            }
        };

        var result = await ctrl.Me() as OkObjectResult;
        Assert.NotNull(result);
        var dto = result!.Value as UserDto;
        Assert.Equal("me@example.com", dto!.Email);
    }

    [Fact]
    public async Task Me_NonExistentUser_ReturnsNotFound()
    {
        var ctrl = BuildControllerWithUser(9999, out _);
        var result = await ctrl.Me();
        Assert.IsType<NotFoundResult>(result);
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidRequest_Returns204()
    {
        var db  = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var cfg = DbHelper.JwtConfig();

        var user = new User { Email = "cp@example.com", FirstName = "C", LastName = "P", Role = "Patient",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctrl = new AuthController(db, jwt, cfg);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Test"))
            }
        };

        var result = await ctrl.ChangePassword(new ChangePasswordRequest("OldPass1!", "NewPass2!"));
        Assert.IsType<NoContentResult>(result);

        var updated = await db.Users.FindAsync(user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass2!", updated!.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        var db  = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var cfg = DbHelper.JwtConfig();

        var user = new User { Email = "cpw@example.com", FirstName = "C", LastName = "P", Role = "Patient",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctrl = new AuthController(db, jwt, cfg);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Test"))
            }
        };

        var result = await ctrl.ChangePassword(new ChangePasswordRequest("WrongOld!", "NewPass2!"));
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
