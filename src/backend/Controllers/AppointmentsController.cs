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
        var userId  = _jwt.GetUserIdFromToken(User);
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Doctor");

        var a = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (a == null) return NotFound();

        // Status transitions allowed by role:
        //   Patient  → "Cancelled" only
        //   Admin/Doctor → any status
        if (req.Status != null)
        {
            if (!isStaff && req.Status != "Cancelled")
                return Forbid();
            a.Status = req.Status;
        }

        if (req.ScheduledAt != null) a.ScheduledAt = req.ScheduledAt.Value;
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

    /// <summary>Get a single doctor's public profile</summary>
    [HttpGet("doctors/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctor(int id)
    {
        var d = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (d == null) return NotFound();

        var schedule = await _db.DoctorSchedules
            .Where(s => s.DoctorId == id && s.IsAvailable)
            .Select(s => new { s.DayOfWeek, StartTime = s.StartTime.ToString("HH:mm"), EndTime = s.EndTime.ToString("HH:mm") })
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();

        var reviews = await _db.DoctorReviews
            .Include(r => r.User)
            .Where(r => r.DoctorId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new {
                r.Id, r.Rating, r.Comment, r.CreatedAt,
                userName = r.User.FirstName + " " + r.User.LastName[0] + "."
            })
            .ToListAsync();

        return Ok(new {
            d.Id, d.Specialty, d.Bio, d.ConsultationFee, d.Rating, d.ReviewCount,
            d.ClinicAddress, d.IsAvailable,
            Name = d.User.FirstName + " " + d.User.LastName,
            schedule,
            reviews
        });
    }

    /// <summary>Submit a review for a doctor (must have a completed appointment)</summary>
    [HttpPost("doctors/{id}/reviews")]
    [Authorize]
    public async Task<IActionResult> SubmitReview(int id, [FromBody] SubmitReviewRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;

        // Verify the appointment belongs to this user, is for this doctor, and is completed
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == req.AppointmentId
                && a.UserId == userId
                && a.DoctorId == id
                && a.Status == "Completed");

        if (appointment == null)
            return BadRequest(new { message = "Không tìm thấy lịch hẹn đã hoàn thành cho bác sĩ này" });

        // One review per appointment
        if (await _db.DoctorReviews.AnyAsync(r => r.AppointmentId == req.AppointmentId))
            return BadRequest(new { message = "Bạn đã đánh giá lịch hẹn này rồi" });

        var review = new DoctorReview
        {
            DoctorId      = id,
            UserId        = userId,
            AppointmentId = req.AppointmentId,
            Rating        = req.Rating,
            Comment       = req.Comment
        };
        _db.DoctorReviews.Add(review);

        // Recalculate doctor rating
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor != null)
        {
            var totalRating = await _db.DoctorReviews.Where(r => r.DoctorId == id).SumAsync(r => (double)r.Rating);
            var count       = await _db.DoctorReviews.CountAsync(r => r.DoctorId == id);
            doctor.Rating      = Math.Round((totalRating + req.Rating) / (count + 1), 1);
            doctor.ReviewCount = count + 1;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cảm ơn bạn đã đánh giá!" });
    }

    /// <summary>Check if current user has already reviewed a given appointment</summary>
    [HttpGet("doctors/{id}/reviews/check")]
    [Authorize]
    public async Task<IActionResult> CheckReview(int id, [FromQuery] int appointmentId)
    {
        var userId   = _jwt.GetUserIdFromToken(User)!.Value;
        var reviewed = await _db.DoctorReviews.AnyAsync(r => r.AppointmentId == appointmentId && r.UserId == userId);
        return Ok(new { reviewed });
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

    /// <summary>List all appointments for the authenticated doctor</summary>
    [HttpGet("doctor")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> GetDoctorAppointments(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        // Admin can view all appointments; Doctor must have a profile
        if (doctor == null && !User.IsInRole("Admin")) return Forbid();

        var query = _db.Appointments
            .Include(a => a.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .AsQueryable();

        // Doctor sees only their appointments; Admin sees all
        if (doctor != null && !User.IsInRole("Admin"))
            query = query.Where(a => a.DoctorId == doctor.Id);

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

    /// <summary>Doctor takes action on an appointment: accept | reschedule | reject</summary>
    [HttpPost("{id}/action")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> TakeAction(int id, [FromBody] DoctorAppointmentActionRequest req)
    {
        var validActions = new[] { "accept", "reschedule", "reject" };
        if (!validActions.Contains(req.Action))
            return BadRequest(new { message = "Action phải là 'accept', 'reschedule' hoặc 'reject'" });

        if (req.Action == "reschedule" && req.RescheduledTo == null)
            return BadRequest(new { message = "Thời gian đổi lịch là bắt buộc" });

        if (req.Action == "reschedule" && string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { message = "Lý do dời lịch là bắt buộc" });

        if (req.Action == "reject" && string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { message = "Lý do từ chối là bắt buộc" });

        var userId = _jwt.GetUserIdFromToken(User)!.Value;
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        var a = await _db.Appointments
            .Include(a => a.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (a == null) return NotFound();

        // Admin can act on any appointment; Doctor can only act on their own
        if (!User.IsInRole("Admin") && a.DoctorId != doctor?.Id)
            return Forbid();

        if (a.Status != "Pending" && a.Status != "Rescheduled")
            return BadRequest(new { message = "Chỉ có thể xử lý đơn đang chờ hoặc đã được dời lịch" });

        switch (req.Action)
        {
            case "accept":
                a.Status = "Confirmed";
                break;
            case "reschedule":
                a.Status          = "Rescheduled";
                a.RescheduleReason = req.Reason;
                a.RescheduledTo   = req.RescheduledTo;
                a.ScheduledAt     = req.RescheduledTo!.Value;
                break;
            case "reject":
                a.Status       = "Cancelled";
                a.CancelReason = req.Reason;
                break;
        }

        a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToDto(a));
    }

    private static AppointmentDto ToDto(Models.Appointment a) => new(
        a.Id, a.UserId,
        a.User?.FirstName + " " + a.User?.LastName,
        a.User?.Email,
        a.User?.PhoneNumber,
        a.DoctorId,
        a.Doctor == null ? null : a.Doctor.User?.FirstName + " " + a.Doctor.User?.LastName,
        a.Doctor?.Specialty,
        a.ScheduledAt, a.Status,
        a.Description, a.Notes, a.Department, a.QueueNumber,
        a.RescheduleReason, a.RescheduledTo, a.CancelReason,
        a.CreatedAt
    );
}
