using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.Models;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Hubs;

[Authorize]
public class DirectChatHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;

    public DirectChatHub(ApplicationDbContext db, JwtService jwt)
    {
        _db  = db;
        _jwt = jwt;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Join a specific chat session room so both parties receive real-time messages.
    /// The caller must be a participant (patient or doctor's user).
    /// </summary>
    public async Task JoinSession(int sessionId)
    {
        var userId = GetUserId();
        if (userId == null) throw new HubException("Unauthorized");

        var session = await _db.DirectChatSessions
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) throw new HubException("Phiên chat không tồn tại");

        if (session.PatientId != userId && session.Doctor.UserId != userId)
            throw new HubException("Bạn không thuộc phiên chat này");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");

        // Mark messages as read
        var unread = await _db.DirectMessages
            .Where(m => m.SessionId == sessionId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();
        foreach (var msg in unread) msg.IsRead = true;
        if (unread.Any()) await _db.SaveChangesAsync();
    }

    /// <summary>Send a message in a session</summary>
    public async Task SendMessage(int sessionId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new HubException("Nội dung không được trống");
        if (content.Length > 4000) throw new HubException("Tin nhắn quá dài");

        var userId = GetUserId();
        if (userId == null) throw new HubException("Unauthorized");

        var session = await _db.DirectChatSessions
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) throw new HubException("Phiên chat không tồn tại");
        if (session.Status == "Closed") throw new HubException("Phiên chat đã đóng");

        if (session.PatientId != userId && session.Doctor.UserId != userId)
            throw new HubException("Bạn không thuộc phiên chat này");

        var sender = await _db.Users.FindAsync(userId);
        var message = new DirectMessage
        {
            SessionId = sessionId,
            SenderId  = userId.Value,
            Content   = content.Trim(),
            SentAt    = DateTime.UtcNow
        };

        _db.DirectMessages.Add(message);
        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var payload = new
        {
            id        = message.Id,
            sessionId,
            senderId  = userId,
            senderName = sender?.FirstName + " " + sender?.LastName,
            content   = message.Content,
            sentAt    = message.SentAt,
            isRead    = false
        };

        await Clients.Group($"session-{sessionId}").SendAsync("ReceiveMessage", payload);
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                 ?? Context.User?.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
