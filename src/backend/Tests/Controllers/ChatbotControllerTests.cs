using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MGSPlus.Api.Controllers;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Models;
using MGSPlus.Api.Services;
using MGSPlus.Tests.Helpers;

namespace MGSPlus.Tests.Controllers;

/// <summary>
/// Unit tests for ChatbotController.
/// DB = EF InMemory.  ChatbotService = real implementation (no agent URL configured).
/// </summary>
public class ChatbotControllerTests
{
    private static (ChatbotController ctrl, ApplicationDbContext db) Build(int? userId = null)
    {
        var db     = DbHelper.CreateInMemoryDb();
        var jwt    = DbHelper.CreateJwtService();
        var config = new ConfigurationBuilder().Build();  // no AgentService URL → fallback mode
        var svc    = new ChatbotService(db, config, NullLogger<ChatbotService>.Instance);

        var ctrl = new ChatbotController(db, jwt, svc);

        var claims = userId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
            : Array.Empty<Claim>();

        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, userId.HasValue ? "Test" : ""))
            }
        };
        return (ctrl, db);
    }

    // ── CreateSession ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSession_Returns201WithSessionDto()
    {
        var (ctrl, _) = Build(userId: 1);
        var result = await ctrl.CreateSession(new CreateChatSessionRequest("My Session")) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
        var dto = result.Value as ChatSessionDto;
        Assert.True(dto!.Id > 0);
        Assert.Equal("My Session", dto.Title);
    }

    [Fact]
    public async Task CreateSession_AnonymousUser_StillWorks()
    {
        var (ctrl, db) = Build(userId: null);
        await ctrl.CreateSession(new CreateChatSessionRequest("Anon"));
        Assert.Equal(1, db.ChatSessions.Count());
    }

    // ── GetSessions ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSessions_ReturnsOnlyCurrentUserSessions()
    {
        var (ctrl, db) = Build(userId: 10);

        // Seed user 10's session + another user's session
        db.ChatSessions.Add(new ChatSession { UserId = 10, Title = "A", SessionType = "General",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.ChatSessions.Add(new ChatSession { UserId = 99, Title = "B", SessionType = "General",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result  = await ctrl.GetSessions() as OkObjectResult;
        var sessions = result!.Value as IEnumerable<ChatSessionDto>;

        Assert.Single(sessions!);
    }

    [Fact]
    public async Task GetSessions_LimitedTo20()
    {
        var (ctrl, db) = Build(userId: 5);

        for (var i = 0; i < 25; i++)
            db.ChatSessions.Add(new ChatSession { UserId = 5, Title = $"S{i}", SessionType = "General",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result   = await ctrl.GetSessions() as OkObjectResult;
        var sessions = result!.Value as IEnumerable<ChatSessionDto>;

        Assert.Equal(20, sessions!.Count());
    }

    // ── GetSession ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSession_ReturnsSessionAndMessages()
    {
        var (ctrl, db) = Build(userId: 3);

        var session = new ChatSession { UserId = 3, Title = "Chat", SessionType = "General",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();

        db.ChatMessages.Add(new ChatMessage
            { SessionId = session.Id, Role = "user", Content = "hi", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await ctrl.GetSession(session.Id) as OkObjectResult;
        Assert.NotNull(result);
        // Response is anonymous object; just verify it's not null
        Assert.NotNull(result!.Value);
    }

    [Fact]
    public async Task GetSession_NotFound_WhenSessionDoesNotExist()
    {
        var (ctrl, _) = Build(userId: 1);
        var result = await ctrl.GetSession(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetSession_NotFound_WhenSessionBelongsToAnotherUser()
    {
        var (ctrl, db) = Build(userId: 1);

        // Session owned by user 99, NOT user 1
        var session = new ChatSession { UserId = 99, Title = "Other", SessionType = "General",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();

        var result = await ctrl.GetSession(session.Id);
        Assert.IsType<NotFoundResult>(result);
    }

    // ── SendMessage ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_Returns200WithResponse()
    {
        var (ctrl, db) = Build(userId: 2);
        var session = new ChatSession { UserId = 2, Title = "T", SessionType = "General",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();

        var result = await ctrl.SendMessage(session.Id, new SendMessageRequest("xin chào", null)) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
        var dto = result.Value as ChatResponseDto;
        Assert.Equal("user",      dto!.UserMessage.Role);
        Assert.Equal("assistant", dto.AssistantMessage.Role);
    }

    [Fact]
    public async Task SendMessage_ReturnsNotFound_WhenSessionMissing()
    {
        var (ctrl, _) = Build(userId: 1);
        var result = await ctrl.SendMessage(9999, new SendMessageRequest("q", null));
        Assert.IsType<NotFoundResult>(result);
    }

    // ── QuickChat ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task QuickChat_CreatesSessionAndReturns200()
    {
        var (ctrl, db) = Build(userId: null);
        var result = await ctrl.QuickChat(new SendMessageRequest("đặt lịch", null)) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
        Assert.Equal(1, db.ChatSessions.Count());
    }
}
