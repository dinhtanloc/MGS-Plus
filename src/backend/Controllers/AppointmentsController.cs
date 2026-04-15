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

    /// <summary>Get the current user's appointments</summary>
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

    /// <summary>Book a new appointment</summary>
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

    /// <summary>Get appointment detail</summary>
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

    /// <summary>Update or cancel an appointment</summary>
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

    /// <summary>Get list of available doctors</summary>
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

    /// <summary>Get a doctor's work schedule</summary>
    [HttpGet("doctors/{id}/schedule")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctorSchedule(int id)
    {
        var exists = await _db.Doctors.AnyAsync(d => d.Id == id);
        if (!exists) return NotFound();

        var schedule = await _db.DoctorSchedules
            .Where(s => s.DoctorId == id && s.IsAvailable)
            .Select(s => new { s.DayOfWeek, StartTime = s.StartTime.ToString("HH:mm"), EndTime = s.EndTime.ToString("HH:mm") })
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();

        return Ok(schedule);
    }

    /// <summary>Get available time slots for a doctor on a given day</summary>
    [HttpGet("doctors/{id}/slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctorSlots(int id, [FromQuery] DateTime date)
    {
        var schedules = await _db.DoctorSchedules
            .Where(s => s.DoctorId == id && s.IsAvailable && s.DayOfWeek == (int)date.DayOfWeek)
            .ToListAsync();

        if (!schedules.Any()) return Ok(new { date, slots = Array.Empty<string>() });

        // Generate 30-minute slots within work hours
        var booked = await _db.Appointments
            .Where(a => a.DoctorId == id && a.ScheduledAt.Date == date.Date && a.Status != "Cancelled")
            .Select(a => a.ScheduledAt.TimeOfDay)
            .ToListAsync();

        var slots = new List<string>();
        foreach (var schedule in schedules)
        {
            var current = schedule.StartTime.ToTimeSpan();
            var end     = schedule.EndTime.ToTimeSpan();
            while (current.Add(TimeSpan.FromMinutes(30)) <= end)
            {
                if (!booked.Any(b => b == current))
                    slots.Add(TimeOnly.FromTimeSpan(current).ToString("HH:mm"));
                current = current.Add(TimeSpan.FromMinutes(30));
            }
        }

        return Ok(new { date = date.ToString("yyyy-MM-dd"), slots });
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
