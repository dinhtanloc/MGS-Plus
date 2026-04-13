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
        _db = db;
        _jwt = jwt;
        _config = config;
    }

    /// <summary>Đăng ký tài khoản mới</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email đã được sử dụng" });

        var user = new User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName = req.FirstName,
            LastName = req.LastName,
            PhoneNumber = req.PhoneNumber,
            Role = "Patient"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Create empty profile
        _db.UserProfiles.Add(new UserProfile { UserId = user.Id });
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        return CreatedAtAction(nameof(Register), new AuthResponse(
            token, "Bearer", expiresMinutes * 60,
            ToDto(user)
        ));
    }

    /// <summary>Đăng nhập</summary>
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

        var token = _jwt.GenerateToken(user);
        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        return Ok(new AuthResponse(token, "Bearer", expiresMinutes * 60, ToDto(user)));
    }

    /// <summary>Lấy thông tin người dùng hiện tại</summary>
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

    /// <summary>Đổi mật khẩu</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.FirstName, u.LastName, u.PhoneNumber, u.Role, u.CreatedAt);
}
