using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Models;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;
    private readonly IConfiguration _config;

    public AuthController(ApplicationDbContext db, JwtService jwt, IConfiguration config)
    {
        _db     = db;
        _jwt    = jwt;
        _config = config;
    }

    /// <summary>Register a new account</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email đã được sử dụng" });

        var user = new User
        {
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName    = req.FirstName,
            LastName     = req.LastName,
            PhoneNumber  = req.PhoneNumber,
            Role         = "Patient"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.UserProfiles.Add(new UserProfile { UserId = user.Id });
        await _db.SaveChangesAsync();

        var (token, refreshToken) = await IssueTokenPairAsync(user);
        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        return CreatedAtAction(nameof(Register), new AuthResponse(
            token, "Bearer", expiresMinutes * 60, ToDto(user), refreshToken));
    }

    /// <summary>Login</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });

        if (!user.IsActive)
            return Unauthorized(new { message = "Tài khoản đã bị khóa" });

        var (token, refreshToken) = await IssueTokenPairAsync(user);
        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        return Ok(new AuthResponse(token, "Bearer", expiresMinutes * 60, ToDto(user), refreshToken));
    }

    /// <summary>Renew access token using a refresh token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        var hashed = JwtService.HashToken(req.RefreshToken);

        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == hashed);

        if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn" });

        if (!stored.User.IsActive)
            return Unauthorized(new { message = "Tài khoản đã bị khóa" });

        // Rotate: revoke old, issue new pair
        stored.IsRevoked = true;
        var (newToken, newRefreshToken) = await IssueTokenPairAsync(stored.User);
        await _db.SaveChangesAsync();

        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");
        return Ok(new AuthResponse(newToken, "Bearer", expiresMinutes * 60, ToDto(stored.User), newRefreshToken));
    }

    /// <summary>Logout — revoke the refresh token</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest req)
    {
        var hashed = JwtService.HashToken(req.RefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == hashed);
        if (stored != null)
        {
            stored.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
        return NoContent();
    }

    /// <summary>Get current authenticated user info</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> Me()
    {
        var userId = _jwt.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(ToDto(user));
    }

    /// <summary>Change password</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.UpdatedAt    = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Send email verification link</summary>
    [HttpPost("send-verification-email")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SendVerificationEmail([FromServices] EmailService email)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (user.IsEmailVerified)
            return BadRequest(new { message = "Email đã được xác thực" });

        await email.SendVerificationEmailAsync(user);
        return NoContent();
    }

    /// <summary>Verify email using the token from the email link</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, [FromServices] EmailService email)
    {
        var userId = email.ValidateVerificationToken(token);
        if (userId == null)
            return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn" });

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.IsEmailVerified = true;
        user.UpdatedAt       = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(string accessToken, string refreshToken)> IssueTokenPairAsync(User user)
    {
        var accessToken = _jwt.GenerateToken(user);
        var (raw, hashed, expiresAt) = _jwt.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token     = hashed,
            UserId    = user.Id,
            ExpiresAt = expiresAt
        });
        await _db.SaveChangesAsync();

        return (accessToken, raw);
    }

    private static UserDto ToDto(User u) =>
        new(u.Id, u.Email, u.FirstName, u.LastName, u.PhoneNumber, u.Role, u.CreatedAt);
}
