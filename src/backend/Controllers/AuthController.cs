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

    private readonly EmailService _email;

    public AuthController(ApplicationDbContext db, JwtService jwt, IConfiguration config, EmailService email)
    {
        _db     = db;
        _jwt    = jwt;
        _config = config;
        _email  = email;
    }

    /// <summary>Register a new account (Patient or Doctor application)</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var errors = new Dictionary<string, string>();

        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            errors["email"] = "Email này đã được đăng ký. Vui lòng sử dụng email khác.";

        var requestedRole = req.RequestedRole ?? "Patient";

        if (requestedRole == "Doctor")
        {
            if (string.IsNullOrWhiteSpace(req.Specialty))
                errors["specialty"] = "Vui lòng chọn chuyên khoa";
            if (string.IsNullOrWhiteSpace(req.LicenseNumber))
                errors["licenseNumber"] = "Vui lòng nhập số giấy phép hành nghề";
            else if (await _db.Doctors.AnyAsync(d => d.LicenseNumber == req.LicenseNumber))
                errors["licenseNumber"] = "Số giấy phép này đã được đăng ký";
        }

        if (errors.Count > 0)
            return BadRequest(new { message = "Vui lòng kiểm tra lại thông tin", errors });

        var user = new User
        {
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName    = req.FirstName,
            LastName     = req.LastName,
            PhoneNumber  = req.PhoneNumber,
            Role         = "Patient" // always starts as Patient; promoted to Doctor after admin approval
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.UserProfiles.Add(new UserProfile { UserId = user.Id });

        bool pendingDoctor = false;
        if (requestedRole == "Doctor")
        {
            _db.Doctors.Add(new Models.Doctor
            {
                UserId            = user.Id,
                Specialty         = req.Specialty!,
                LicenseNumber     = req.LicenseNumber!,
                Bio               = req.Bio,
                ConsultationFee   = req.ConsultationFee ?? 0,
                ApplicationStatus = "Pending",
                IsAvailable       = false // not available until approved
            });
            pendingDoctor = true;
        }

        await _db.SaveChangesAsync();

        var (token, refreshToken) = await IssueTokenPairAsync(user);
        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        // Fire-and-forget: send verification email without blocking registration
        _ = Task.Run(async () =>
        {
            try { await _email.SendVerificationEmailAsync(user); }
            catch { /* swallow — email failure must not break registration */ }
        });

        return CreatedAtAction(nameof(Register), new
        {
            token,
            tokenType    = "Bearer",
            expiresIn    = expiresMinutes * 60,
            user         = ToDto(user),
            refreshToken,
            pendingDoctor // frontend uses this to show "awaiting approval" message
        });
    }

    /// <summary>Login</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var errors = new Dictionary<string, string>();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        
        if (user == null)
            errors["email"] = "Email không tồn tại trong hệ thống";
        else if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            errors["password"] = "Mật khẩu không chính xác";
        
        if (errors.Count > 0)
            return Unauthorized(new { message = "Đăng nhập thất bại", errors });

        if (!user.IsActive)
            return Unauthorized(new { message = "Tài khoản đã bị khóa", errors = new Dictionary<string, string>() });

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

    /// <summary>Send email verification link (resend)</summary>
    [HttpPost("send-verification-email")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SendVerificationEmail()
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (user.IsEmailVerified)
            return BadRequest(new { message = "Email đã được xác thực" });

        await _email.SendVerificationEmailAsync(user);
        return NoContent();
    }

    /// <summary>Verify email via link in email — redirects browser to frontend</summary>
    [HttpGet("verify-email")]
    [ProducesResponseType(302)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var frontendBase = _config["App:FrontendUrl"] ?? "http://localhost:3000";

        var userId = _email.ValidateVerificationToken(token);
        if (userId == null)
            return Redirect($"{frontendBase}/verify-email?status=expired");

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return Redirect($"{frontendBase}/verify-email?status=expired");

        if (!user.IsEmailVerified)
        {
            user.IsEmailVerified = true;
            user.UpdatedAt       = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Redirect($"{frontendBase}/verify-email?status=success");
    }

    /// <summary>Verify email via API (JSON) — for admin or programmatic use</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmailApi([FromQuery] string token)
    {
        var userId = _email.ValidateVerificationToken(token);
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
