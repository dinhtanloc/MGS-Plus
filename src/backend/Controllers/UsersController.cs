using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;

    public UsersController(ApplicationDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>Get the full user profile</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var user = await _db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id, user.Email, user.FirstName, user.LastName,
            user.PhoneNumber, user.DateOfBirth, user.Role, user.IsEmailVerified, user.CreatedAt,
            profile = user.Profile == null ? null : new
            {
                user.Profile.AvatarUrl, user.Profile.Address,
                user.Profile.InsuranceNumber, user.Profile.InsuranceProvider,
                user.Profile.InsuranceStartDate, user.Profile.InsuranceEndDate,
                user.Profile.BloodType, user.Profile.Allergies, user.Profile.ChronicDiseases
            }
        });
    }

    /// <summary>Update personal information and profile</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var user = await _db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        if (req.FirstName != null) user.FirstName = req.FirstName;
        if (req.LastName != null) user.LastName = req.LastName;
        if (req.PhoneNumber != null) user.PhoneNumber = req.PhoneNumber;
        if (req.DateOfBirth != null) user.DateOfBirth = req.DateOfBirth;
        user.UpdatedAt = DateTime.UtcNow;

        if (user.Profile == null)
        {
            user.Profile = new() { UserId = user.Id };
            _db.UserProfiles.Add(user.Profile);
        }

        if (req.Address != null) user.Profile.Address = req.Address;
        if (req.InsuranceNumber != null) user.Profile.InsuranceNumber = req.InsuranceNumber;
        if (req.InsuranceProvider != null) user.Profile.InsuranceProvider = req.InsuranceProvider;
        if (req.BloodType != null) user.Profile.BloodType = req.BloodType;
        if (req.Allergies != null) user.Profile.Allergies = req.Allergies;
        if (req.ChronicDiseases != null) user.Profile.ChronicDiseases = req.ChronicDiseases;
        user.Profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get all users (Admin only)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Users.AsQueryable();
        var total = await query.CountAsync();
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.CreatedAt })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = users });
    }
}
