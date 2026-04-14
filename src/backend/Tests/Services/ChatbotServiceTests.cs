using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Models;
using MGSPlus.Api.Services;
using MGSPlus.Tests.Helpers;

namespace MGSPlus.Tests.Services;

/// <summary>
/// Unit tests for ChatbotService.
/// Uses EF InMemory for the DB layer.
/// AgentService is NOT configured (no URL) so tests exercise the fallback path.
/// </summary>
public class ChatbotServiceTests
{
    private static ChatbotService BuildService(out ApplicationDbContext db, string? agentUrl = null)
    {
        db = DbHelper.CreateInMemoryDb();
        var configDict = new Dictionary<string, string?>();
        if (agentUrl is not null)
            configDict["AgentService:SupervisorUrl"] = agentUrl;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        return new ChatbotService(db, config, NullLogger<ChatbotService>.Instance);
    }

    private static async Task<ChatSession> SeedSession(ApplicationDbContext db, int? userId = null)
    {
        var svc = new ChatbotService(db,
            new ConfigurationBuilder().Build(),
            NullLogger<ChatbotService>.Instance);
        return await svc.CreateSessionAsync(userId, new CreateChatSessionRequest("Test"));
    }

    // ── CreateSessionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSession_PersistsToDb_WithDefaultTitle()
    {
        var svc = BuildService(out var db);
        var req = new CreateChatSessionRequest(null);

        var session = await svc.CreateSessionAsync(null, req);

        Assert.True(session.Id > 0);
        Assert.Equal("Cuộc trò chuyện mới", session.Title);
        Assert.Equal(1, db.ChatSessions.Count());
    }

    [Fact]
    public async Task CreateSession_UsesProvidedTitle()
    {
        var svc = BuildService(out _);
        var session = await svc.CreateSessionAsync(null, new CreateChatSessionRequest("My Chat"));
        Assert.Equal("My Chat", session.Title);
    }

    [Fact]
    public async Task CreateSession_AssociatesWithUserId()
    {
        var svc = BuildService(out var db);
        var session = await svc.CreateSessionAsync(7, new CreateChatSessionRequest("U7 Chat"));
        Assert.Equal(7, db.ChatSessions.Find(session.Id)!.UserId);
    }

    [Fact]
    public async Task CreateSession_NullUserId_AllowedForAnonymous()
    {
        var svc = BuildService(out var db);
        var session = await svc.CreateSessionAsync(null, new CreateChatSessionRequest("Anon"));
        Assert.Null(db.ChatSessions.Find(session.Id)!.UserId);
    }

    // ── SendMessageAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_PersistsUserAndAssistantMessages()
    {
        var svc = BuildService(out var db);
        var session = await SeedSession(db);

        var response = await svc.SendMessageAsync(session.Id, null, new SendMessageRequest("hello", null));

        Assert.Equal("user",      response.UserMessage.Role);
        Assert.Equal("hello",     response.UserMessage.Content);
        Assert.Equal("assistant", response.AssistantMessage.Role);
        Assert.NotEmpty(response.AssistantMessage.Content);

        Assert.Equal(2, db.ChatMessages.Count(m => m.SessionId == session.Id));
    }

    [Fact]
    public async Task SendMessage_UpdatesSessionUpdatedAt()
    {
        var svc = BuildService(out var db);
        var session = await SeedSession(db);
        var before  = session.UpdatedAt;

        // Ensure measurable time difference
        await Task.Delay(10);
        await svc.SendMessageAsync(session.Id, null, new SendMessageRequest("tick", null));

        var updated = db.ChatSessions.Find(session.Id)!;
        Assert.True(updated.UpdatedAt >= before);
    }

    [Fact]
    public async Task SendMessage_ThrowsKeyNotFound_WhenSessionMissing()
    {
        var svc = BuildService(out _);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.SendMessageAsync(9999, null, new SendMessageRequest("q", null)));
    }

    // ── Fallback responses ────────────────────────────────────────────────────

    [Theory]
    [InlineData("bảo hiểm y tế",   "bảo hiểm")]
    [InlineData("BHYT",             "bảo hiểm")]
    [InlineData("đặt lịch khám",    "Đặt lịch")]
    [InlineData("xin chào",         "trợ lý")]
    [InlineData("câu hỏi khác",     "hệ thống AI")]
    public async Task SendMessage_FallbackContent_ContainsExpectedKeyword(string userMsg, string expectedKeyword)
    {
        var svc = BuildService(out var db);
        var session = await SeedSession(db);

        var response = await svc.SendMessageAsync(session.Id, null, new SendMessageRequest(userMsg, null));

        Assert.Contains(expectedKeyword, response.AssistantMessage.Content, StringComparison.OrdinalIgnoreCase);
    }

    // ── StreamMessageAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task StreamMessage_ThrowsKeyNotFound_WhenSessionMissing()
    {
        var svc = BuildService(out _);

        async Task Act()
        {
            await foreach (var _ in svc.StreamMessageAsync(9999, null, new SendMessageRequest("q", null)))
            { }
        }

        await Assert.ThrowsAsync<KeyNotFoundException>(Act);
    }

    [Fact]
    public async Task StreamMessage_EmitsFallback_WhenNoAgentUrl()
    {
        var svc = BuildService(out var db);
        var session = await SeedSession(db);

        var events = new List<string>();
        await foreach (var e in svc.StreamMessageAsync(session.Id, null, new SendMessageRequest("xin chào", null)))
            events.Add(e);

        Assert.Contains(events, e => e.Contains("session"));
        Assert.Contains(events, e => e.Contains("response_chunk") || e.Contains("complete"));
    }

    [Fact]
    public async Task StreamMessage_PersistsAssistantMessage_AfterStream()
    {
        var svc = BuildService(out var db);
        var session = await SeedSession(db);

        await foreach (var _ in svc.StreamMessageAsync(session.Id, null, new SendMessageRequest("đặt lịch", null)))
        { }

        // user msg + assistant msg
        Assert.Equal(2, db.ChatMessages.Count(m => m.SessionId == session.Id));
        var assistant = db.ChatMessages.First(m => m.Role == "assistant" && m.SessionId == session.Id);
        Assert.NotEmpty(assistant.Content);
    }
}
