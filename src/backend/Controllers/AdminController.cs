using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;

    public AdminController(ApplicationDbContext db, JwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <summary>System statistics for admin dashboard</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now   = DateTime.UtcNow;
        var stats = new AdminStatsDto(
            TotalUsers:             await _db.Users.CountAsync(),
            TotalDoctors:           await _db.Users.CountAsync(u => u.Role == "Doctor"),
            TotalPatients:          await _db.Users.CountAsync(u => u.Role == "Patient"),
            PendingDoctors:         await _db.Doctors.CountAsync(d => d.ApplicationStatus == "Pending"),
            TotalAppointments:      await _db.Appointments.CountAsync(),
            TotalBlogPosts:         await _db.BlogPosts.CountAsync(),
            TotalNews:              await _db.News.CountAsync(),
            AppointmentsThisMonth:  await _db.Appointments.CountAsync(
                                        a => a.CreatedAt.Year == now.Year && a.CreatedAt.Month == now.Month),
            AppointmentsByMonth:    (await _db.Appointments
                                        .Where(a => a.CreatedAt >= now.AddMonths(-5))
                                        .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
                                        .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                                        .OrderBy(m => m.Year).ThenBy(m => m.Month)
                                        .ToListAsync())
                                        .Select(m => new MonthlyStatDto(m.Year, m.Month, m.Count))
                                        .ToList()
        );
        return Ok(stats);
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    /// <summary>Rich analytics data for admin dashboard charts</summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var now   = DateTime.UtcNow;
        var day30 = now.AddDays(-29);
        var day7  = now.AddDays(-6);

        // ── 30-day user registrations (daily) ─────────────────────────────────
        var regRaw = await _db.Users
            .Where(u => u.CreatedAt >= day30)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill missing days with 0
        var registrations = Enumerable.Range(0, 30)
            .Select(i => day30.Date.AddDays(i))
            .Select(d => new
            {
                date  = d.ToString("yyyy-MM-dd"),
                count = regRaw.FirstOrDefault(r => r.Date == d)?.Count ?? 0
            }).ToList();

        // ── 30-day chat/AI sessions (daily) ───────────────────────────────────
        var chatRaw = await _db.ChatSessions
            .Where(s => s.CreatedAt >= day30)
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var chatActivity = Enumerable.Range(0, 30)
            .Select(i => day30.Date.AddDays(i))
            .Select(d => new
            {
                date  = d.ToString("yyyy-MM-dd"),
                count = chatRaw.FirstOrDefault(r => r.Date == d)?.Count ?? 0
            }).ToList();

        // ── AI token usage (by model, last 30 days) ───────────────────────────
        var tokenByModel = await _db.ChatMessages
            .Where(m => m.CreatedAt >= day30 && m.TokensUsed != null && m.Role == "assistant")
            .GroupBy(m => m.Model ?? "unknown")
            .Select(g => new { Model = g.Key, Tokens = g.Sum(m => m.TokensUsed ?? 0), Calls = g.Count() })
            .OrderByDescending(x => x.Tokens)
            .ToListAsync();

        var totalTokens = tokenByModel.Sum(t => t.Tokens);

        // ── Appointment status breakdown ──────────────────────────────────────
        var apptByStatus = await _db.Appointments
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // ── User role distribution ────────────────────────────────────────────
        var roleDistribution = await _db.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync();

        // ── Direct chat sessions stats ────────────────────────────────────────
        var directChatTotal  = await _db.DirectChatSessions.CountAsync();
        var directMsgTotal   = await _db.DirectMessages.CountAsync();
        var directChatToday  = await _db.DirectMessages
            .CountAsync(m => m.SentAt.Date == now.Date);

        // ── 7-day direct messages (daily) ─────────────────────────────────────
        var dmRaw = await _db.DirectMessages
            .Where(m => m.SentAt >= day7)
            .GroupBy(m => m.SentAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var directMessages7d = Enumerable.Range(0, 7)
            .Select(i => day7.Date.AddDays(i))
            .Select(d => new
            {
                date  = d.ToString("MM/dd"),
                count = dmRaw.FirstOrDefault(r => r.Date == d)?.Count ?? 0
            }).ToList();

        // ── Prescription OCR stats ────────────────────────────────────────────
        var prescriptions = new
        {
            total     = await _db.Prescriptions.CountAsync(),
            processed = await _db.Prescriptions.CountAsync(p => p.Status == "Processed"),
            failed    = await _db.Prescriptions.CountAsync(p => p.Status == "Failed"),
            pending   = await _db.Prescriptions.CountAsync(p => p.Status == "Pending"),
        };

        // ── Recent chat sessions (doctor-patient) ─────────────────────────────
        var recentDirectChats = await _db.DirectChatSessions
            .Include(s => s.Patient)
            .Include(s => s.Doctor).ThenInclude(d => d.User)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(10)
            .Select(s => new
            {
                id          = s.Id,
                patientName = s.Patient.FirstName + " " + s.Patient.LastName,
                doctorName  = s.Doctor.User.FirstName + " " + s.Doctor.User.LastName,
                specialty   = s.Doctor.Specialty,
                msgCount    = s.Messages.Count(),
                status      = s.Status,
                updatedAt   = s.UpdatedAt
            })
            .ToListAsync();

        // ── Recent AI chat sessions ───────────────────────────────────────────
        var recentAiChats = await _db.ChatSessions
            .Include(s => s.User)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(10)
            .Select(s => new
            {
                id          = s.Id,
                title       = s.Title ?? "Phiên không tiêu đề",
                userName    = s.User != null ? s.User.FirstName + " " + s.User.LastName : "Ẩn danh",
                sessionType = s.SessionType,
                msgCount    = s.Messages.Count(),
                updatedAt   = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            registrations,
            chatActivity,
            tokenByModel,
            totalTokens,
            apptByStatus,
            roleDistribution,
            directChat = new { total = directChatTotal, totalMessages = directMsgTotal, todayMessages = directChatToday },
            directMessages7d,
            prescriptions,
            recentDirectChats,
            recentAiChats,
        });
    }

    // ── Doctor Applications ───────────────────────────────────────────────────

    /// <summary>List doctor applications (filter by status)</summary>
    [HttpGet("doctor-applications")]
    public async Task<IActionResult> GetDoctorApplications([FromQuery] string? status)
    {
        var query = _db.Doctors.Include(d => d.User).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.ApplicationStatus == status);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DoctorApplicationDto(
                d.Id, d.UserId,
                d.User.FirstName + " " + d.User.LastName,
                d.User.Email,
                d.Specialty, d.LicenseNumber, d.Bio, d.ConsultationFee,
                d.ApplicationStatus, d.RejectionReason,
                d.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Approve or reject a doctor application</summary>
    [HttpPost("doctor-applications/{id}/review")]
    public async Task<IActionResult> ReviewDoctorApplication(
        int id, [FromBody] ReviewDoctorApplicationRequest req)
    {
        if (req.Action != "approve" && req.Action != "reject")
            return BadRequest(new { message = "Action phải là 'approve' hoặc 'reject'" });

        if (req.Action == "reject" && string.IsNullOrWhiteSpace(req.RejectionReason))
            return BadRequest(new { message = "Lý do từ chối là bắt buộc" });

        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null) return NotFound();

        if (doctor.ApplicationStatus != "Pending")
            return BadRequest(new { message = "Đơn này đã được xử lý" });

        var reviewerId = _jwt.GetUserIdFromToken(User)!.Value;

        doctor.ReviewedAt        = DateTime.UtcNow;
        doctor.ReviewedByUserId  = reviewerId;

        if (req.Action == "approve")
        {
            doctor.ApplicationStatus = "Approved";
            doctor.IsAvailable       = true;
            doctor.User.Role         = "Doctor";
        }
        else
        {
            doctor.ApplicationStatus = "Rejected";
            doctor.RejectionReason   = req.RejectionReason;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── User Management ───────────────────────────────────────────────────────

    /// <summary>List all users with search and pagination</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.Email.Contains(search) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(
                u.Id, u.Email, u.FirstName, u.LastName,
                u.Role, u.IsActive, u.IsEmailVerified, u.CreatedAt))
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = users });
    }

    /// <summary>Grant Admin role to a user</summary>
    [HttpPost("users/{id}/grant-admin")]
    public async Task<IActionResult> GrantAdmin(int id)
    {
        var currentUserId = _jwt.GetUserIdFromToken(User)!.Value;
        if (id == currentUserId)
            return BadRequest(new { message = "Không thể tự cấp quyền cho bản thân" });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (user.Role == "Admin")
            return BadRequest(new { message = "Người dùng đã có quyền Admin" });

        user.Role      = "Admin";
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Revoke Admin role from a user</summary>
    [HttpPost("users/{id}/revoke-admin")]
    public async Task<IActionResult> RevokeAdmin(int id)
    {
        var currentUserId = _jwt.GetUserIdFromToken(User)!.Value;
        if (id == currentUserId)
            return BadRequest(new { message = "Không thể tự thu hồi quyền Admin của bản thân" });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (user.Role != "Admin")
            return BadRequest(new { message = "Người dùng không có quyền Admin" });

        // Revert to Doctor if they have an approved Doctor record, otherwise Patient
        var hasDoctor = await _db.Doctors.AnyAsync(d => d.UserId == id && d.ApplicationStatus == "Approved");
        user.Role      = hasDoctor ? "Doctor" : "Patient";
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Manually mark a user's email as verified</summary>
    [HttpPost("users/{id}/verify-email")]
    public async Task<IActionResult> AdminVerifyEmail(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsEmailVerified = true;
        user.UpdatedAt       = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Resend verification email to a user</summary>
    [HttpPost("users/{id}/resend-verification")]
    public async Task<IActionResult> AdminResendVerification(int id, [FromServices] EmailService email)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.IsEmailVerified)
            return BadRequest(new { message = "Email đã được xác thực" });

        _ = Task.Run(async () =>
        {
            try { await email.SendVerificationEmailAsync(user); }
            catch { /* swallow */ }
        });

        return NoContent();
    }

    /// <summary>Toggle user active/inactive status</summary>
    [HttpPut("users/{id}/toggle-active")]
    public async Task<IActionResult> ToggleUserActive(int id)
    {
        var currentUserId = _jwt.GetUserIdFromToken(User)!.Value;
        if (id == currentUserId)
            return BadRequest(new { message = "Không thể khóa tài khoản của bản thân" });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive  = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { isActive = user.IsActive });
    }
}
