using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.Models;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Controllers;

[ApiController]
[Route("api/direct-chat")]
[Authorize]
[Produces("application/json")]
public class DirectChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;

    public DirectChatController(ApplicationDbContext db, JwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    /// <summary>Get or create a chat session between patient and doctor</summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> GetOrCreateSession([FromBody] CreateSessionRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;

        // Determine patient/doctor sides
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == req.DoctorId);
        if (doctor == null) return NotFound(new { message = "Bác sĩ không tồn tại" });

        // The caller must be the patient (cannot start a session with yourself)
        if (userId == doctor.UserId)
            return BadRequest(new { message = "Bác sĩ không thể tự tạo phiên chat với mình" });

        var existing = await _db.DirectChatSessions
            .FirstOrDefaultAsync(s => s.PatientId == userId && s.DoctorId == req.DoctorId);

        if (existing != null)
            return Ok(SessionDto(existing));

        var session = new DirectChatSession
        {
            PatientId = userId,
            DoctorId  = req.DoctorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.DirectChatSessions.Add(session);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, SessionDto(session));
    }

    /// <summary>List my chat sessions (both patient and doctor sides)</summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetMySessions()
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;
        var isDoctor = User.IsInRole("Doctor");

        IQueryable<DirectChatSession> query = isDoctor
            ? _db.DirectChatSessions.Include(s => s.Patient).Include(s => s.Doctor).ThenInclude(d => d.User)
                 .Where(s => s.Doctor.UserId == userId)
            : _db.DirectChatSessions.Include(s => s.Patient).Include(s => s.Doctor).ThenInclude(d => d.User)
                 .Where(s => s.PatientId == userId);

        var sessions = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new
            {
                id              = s.Id,
                patientId       = s.PatientId,
                patientName     = s.Patient.FirstName + " " + s.Patient.LastName,
                patientAvatar   = (string?)null,
                doctorId        = s.DoctorId,
                doctorName      = s.Doctor.User.FirstName + " " + s.Doctor.User.LastName,
                doctorSpecialty = s.Doctor.Specialty,
                status          = s.Status,
                updatedAt       = s.UpdatedAt,
                unreadCount     = s.Messages.Count(m => !m.IsRead && m.SenderId != userId)
            })
            .ToListAsync();

        return Ok(sessions);
    }

    /// <summary>Get a specific session (must be participant)</summary>
    [HttpGet("sessions/{id}")]
    public async Task<IActionResult> GetSession(int id)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;
        var session = await _db.DirectChatSessions
            .Include(s => s.Patient)
            .Include(s => s.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();
        if (session.PatientId != userId && session.Doctor.UserId != userId) return Forbid();

        return Ok(SessionDto(session));
    }

    /// <summary>Get message history for a session (paginated)</summary>
    [HttpGet("sessions/{id}/messages")]
    public async Task<IActionResult> GetMessages(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;
        var session = await _db.DirectChatSessions
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();
        if (session.PatientId != userId && session.Doctor.UserId != userId) return Forbid();

        var total = await _db.DirectMessages.CountAsync(m => m.SessionId == id);
        var messages = await _db.DirectMessages
            .Include(m => m.Sender)
            .Where(m => m.SessionId == id)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                id         = m.Id,
                sessionId  = m.SessionId,
                senderId   = m.SenderId,
                senderName = m.Sender.FirstName + " " + m.Sender.LastName,
                content    = m.Content,
                sentAt     = m.SentAt,
                isRead     = m.IsRead
            })
            .ToListAsync();

        // Mark as read
        var unread = await _db.DirectMessages
            .Where(m => m.SessionId == id && m.SenderId != userId && !m.IsRead)
            .ToListAsync();
        foreach (var msg in unread) msg.IsRead = true;
        if (unread.Any()) await _db.SaveChangesAsync();

        return Ok(new { total, page, pageSize, data = messages.OrderBy(m => m.sentAt) });
    }

    private static object SessionDto(DirectChatSession s) => new
    {
        id        = s.Id,
        patientId = s.PatientId,
        doctorId  = s.DoctorId,
        status    = s.Status,
        createdAt = s.CreatedAt,
        updatedAt = s.UpdatedAt
    };
}

public record CreateSessionRequest(int DoctorId);
