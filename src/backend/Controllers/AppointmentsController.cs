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
public class AppointmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;

    public AppointmentsController(ApplicationDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>Lấy danh sách lịch hẹn của người dùng</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyAppointments([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var query = _db.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Where(a => a.UserId == userId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = items.Select(ToDto) });
    }

    /// <summary>Đặt lịch hẹn mới</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;

        // Assign queue number for the day
        var sameDay = await _db.Appointments
            .Where(a => a.ScheduledAt.Date == req.ScheduledAt.Date && a.Status != "Cancelled")
            .CountAsync();

        var appointment = new Models.Appointment
        {
            UserId = userId,
            DoctorId = req.DoctorId,
            ScheduledAt = req.ScheduledAt,
            Department = req.Department,
            Description = req.Description,
            Status = "Pending",
            QueueNumber = sameDay + 1
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var created = await _db.Appointments
            .Include(a => a.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .FirstAsync(a => a.Id == appointment.Id);

        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, ToDto(created));
    }

    /// <summary>Lấy chi tiết lịch hẹn</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var a = await _db.Appointments
            .Include(a => a.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (a == null) return NotFound();
        return Ok(ToDto(a));
    }

    /// <summary>Cập nhật / hủy lịch hẹn</summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var a = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (a == null) return NotFound();

        if (req.ScheduledAt != null) a.ScheduledAt = req.ScheduledAt.Value;
        if (req.Status != null) a.Status = req.Status;
        if (req.Notes != null) a.Notes = req.Notes;
        if (req.Department != null) a.Department = req.Department;
        a.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Lấy danh sách bác sĩ</summary>
    [HttpGet("doctors")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctors([FromQuery] string? specialty)
    {
        var query = _db.Doctors
            .Include(d => d.User)
            .Where(d => d.IsAvailable);

        if (!string.IsNullOrEmpty(specialty))
            query = query.Where(d => d.Specialty.Contains(specialty));

        var doctors = await query.Select(d => new
        {
            d.Id, d.Specialty, d.Bio, d.ConsultationFee, d.Rating, d.ReviewCount,
            Name = d.User.FirstName + " " + d.User.LastName,
            d.User.Email
        }).ToListAsync();

        return Ok(doctors);
    }

    private static AppointmentDto ToDto(Models.Appointment a) => new(
        a.Id, a.UserId,
        a.User?.FirstName + " " + a.User?.LastName,
        a.DoctorId,
        a.Doctor == null ? null : a.Doctor.User?.FirstName + " " + a.Doctor.User?.LastName,
        a.Doctor?.Specialty,
        a.ScheduledAt, a.Status,
        a.Description, a.Notes, a.Department, a.QueueNumber, a.CreatedAt
    );
}
