using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatbotController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;
    private readonly ChatbotService _chatbot;

    public ChatbotController(ApplicationDbContext db, JwtService jwt, ChatbotService chatbot)
    {
        _db = db;
        _jwt = jwt;
        _chatbot = chatbot;
    }

    /// <summary>Create a new chat session</summary>
    [HttpPost("sessions")]
    [Authorize]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var session = await _chatbot.CreateSessionAsync(userId, req);
        return CreatedAtAction(nameof(GetSession), new { id = session.Id },
            new ChatSessionDto(session.Id, session.Title, session.SessionType, 0, session.CreatedAt, session.UpdatedAt));
    }

    /// <summary>Get the current user's chat sessions</summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions()
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var sessions = await _db.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new ChatSessionDto(
                s.Id, s.Title, s.SessionType,
                s.Messages.Count, s.CreatedAt, s.UpdatedAt))
            .Take(20)
            .ToListAsync();

        return Ok(sessions);
    }

    /// <summary>Chat session detail with message history</summary>
    [HttpGet("sessions/{id}")]
    [Authorize]
    public async Task<IActionResult> GetSession(int id)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var session = await _db.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id && (s.UserId == null || s.UserId == userId));

        if (session == null) return NotFound();

        return Ok(new
        {
            session = new ChatSessionDto(session.Id, session.Title, session.SessionType,
                session.Messages.Count, session.CreatedAt, session.UpdatedAt),
            messages = session.Messages.Select(m => new ChatMessageDto(m.Id, m.Role, m.Content, m.CreatedAt))
        });
    }

    /// <summary>Send a message to the chatbot and get a response</summary>
    [HttpPost("sessions/{id}/messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        try
        {
            var response = await _chatbot.SendMessageAsync(id, userId, req);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Streaming chat — returns SSE events with agent reasoning steps</summary>
    [HttpPost("sessions/{id}/messages/stream")]
    [Authorize]
    public async Task StreamMessage(int id, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var userId = _jwt.GetUserIdFromToken(User);

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // disable nginx buffering
        Response.Headers["Connection"] = "keep-alive";

        try
        {
            await foreach (var eventData in _chatbot.StreamMessageAsync(id, userId, req, ct))
            {
                await Response.WriteAsync($"data: {eventData}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (KeyNotFoundException)
        {
            await Response.WriteAsync("data: {\"type\":\"error\",\"content\":\"Session not found\"}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — normal
        }
    }

    /// <summary>Quick chat without creating a session first</summary>
    [HttpPost("quick")]
    [Authorize]
    public async Task<IActionResult> QuickChat([FromBody] SendMessageRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var session = await _chatbot.CreateSessionAsync(userId, new CreateChatSessionRequest("Quick Chat"));
        var response = await _chatbot.SendMessageAsync(session.Id, userId, req);
        return Ok(new { sessionId = session.Id, response });
    }
}
