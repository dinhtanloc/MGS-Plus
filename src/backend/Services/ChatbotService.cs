using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MGSPlus.Api.Services;

public class ChatbotService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(ApplicationDbContext db, IConfiguration config, ILogger<ChatbotService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<ChatSession> CreateSessionAsync(int? userId, CreateChatSessionRequest req)
    {
        var session = new ChatSession
        {
            UserId = userId,
            Title = req.Title ?? "Cuộc trò chuyện mới",
            SessionType = req.SessionType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<ChatResponseDto> SendMessageAsync(int sessionId, int? userId, SendMessageRequest req)
    {
        var session = await _db.ChatSessions
            .Include(s => s.Messages.OrderByDescending(m => m.CreatedAt).Take(10))
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException("Session not found");

        var userMsg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "user",
            Content = req.Content,
            CreatedAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(userMsg);

        // Build AI response — connects to agent service
        var assistantContent = await GenerateResponseAsync(req.Content, sessionId, userId);

        var assistantMsg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "assistant",
            Content = assistantContent,
            Model = "mgsplus-agent",
            CreatedAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(assistantMsg);

        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ChatResponseDto(
            new ChatMessageDto(userMsg.Id, userMsg.Role, userMsg.Content, userMsg.CreatedAt),
            new ChatMessageDto(assistantMsg.Id, assistantMsg.Role, assistantMsg.Content, assistantMsg.CreatedAt)
        );
    }

    // ── Streaming ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stream SSE events from the agent service back to the caller.
    /// Saves user message immediately; saves assistant message after the stream completes.
    /// </summary>
    public async IAsyncEnumerable<string> StreamMessageAsync(
        int sessionId,
        int? userId,
        SendMessageRequest req,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new KeyNotFoundException("Session not found");

        // Persist user message upfront so the client can correlate IDs
        var userMsg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "user",
            Content = req.Content,
            CreatedAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(userMsg);
        await _db.SaveChangesAsync(ct);

        yield return JsonSerializer.Serialize(new { type = "session", userMessageId = userMsg.Id, sessionId });

        var responseBuilder = new StringBuilder();
        var agentUrl = _config["AgentService:SupervisorUrl"];

        if (!string.IsNullOrEmpty(agentUrl))
        {
            // ── Try to connect to agent stream ────────────────────────────
            HttpResponseMessage? agentResp = null;
            bool connectOk = false;

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
                var agentApiKey = _config["AgentService:ApiKey"] ?? "";
                var payload = new
                {
                    question = req.Content,
                    thread_id = sessionId.ToString(),
                    user_id = userId?.ToString() ?? "anonymous"
                };
                var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{agentUrl}/chat/stream")
                {
                    Content = JsonContent.Create(payload)
                };
                if (!string.IsNullOrEmpty(agentApiKey))
                    httpReq.Headers.Add("X-Api-Key", agentApiKey);
                agentResp = await http.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);
                connectOk = agentResp.IsSuccessStatusCode;
                if (!connectOk) { agentResp.Dispose(); agentResp = null; }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent stream unavailable, using fallback");
            }

            if (connectOk && agentResp != null)
            {
                // ── Forward agent SSE to client line by line ──────────────
                Stream? bodyStream = null;
                StreamReader? reader = null;
                bool streamOpenOk = false;

                try
                {
                    bodyStream = await agentResp.Content.ReadAsStreamAsync(ct);
                    reader = new StreamReader(bodyStream);
                    streamOpenOk = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to open agent response stream");
                }

                if (streamOpenOk && reader != null)
                {
                    while (!reader.EndOfStream && !ct.IsCancellationRequested)
                    {
                        string? line = null;
                        bool readOk = true;
                        try { line = await reader.ReadLineAsync(ct); }
                        catch { readOk = false; }

                        if (!readOk) break;
                        if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

                        var eventJson = line[6..]; // strip "data: " prefix

                        // Buffer answer content to persist after stream ends
                        try
                        {
                            using var doc = JsonDocument.Parse(eventJson);
                            if (doc.RootElement.TryGetProperty("type", out var tp))
                            {
                                var t = tp.GetString();
                                if ((t == "answer" || t == "response_chunk")
                                    && doc.RootElement.TryGetProperty("content", out var cp))
                                    responseBuilder.Append(cp.GetString());
                            }
                        }
                        catch { /* malformed event — skip */ }

                        yield return eventJson;
                    }

                    reader.Dispose();
                    await bodyStream!.DisposeAsync();
                }

                agentResp.Dispose();
            }
        }

        // ── Fallback when agent produced nothing ──────────────────────────
        if (responseBuilder.Length == 0)
        {
            var fallback = FallbackResponse(req.Content);
            responseBuilder.Append(fallback);
            yield return JsonSerializer.Serialize(new { type = "response_chunk", content = fallback });
        }

        // ── Persist assistant message ─────────────────────────────────────
        var assistantMsg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "assistant",
            Content = responseBuilder.ToString(),
            Model = "mgsplus-agent",
            CreatedAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(assistantMsg);
        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        yield return JsonSerializer.Serialize(new { type = "complete", messageId = assistantMsg.Id });
    }

    // ── Non-streaming ────────────────────────────────────────────────────────

    private async Task<string> GenerateResponseAsync(string userMessage, int sessionId, int? userId)
    {
        var agentUrl = _config["AgentService:SupervisorUrl"];
        if (!string.IsNullOrEmpty(agentUrl))
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var agentApiKey = _config["AgentService:ApiKey"] ?? "";
                var payload = new
                {
                    question = userMessage,
                    thread_id = sessionId.ToString(),
                    user_id = userId?.ToString() ?? "anonymous"
                };
                using var req2 = new HttpRequestMessage(HttpMethod.Post, $"{agentUrl}/chat")
                {
                    Content = JsonContent.Create(payload)
                };
                if (!string.IsNullOrEmpty(agentApiKey))
                    req2.Headers.Add("X-Api-Key", agentApiKey);
                var resp = await http.SendAsync(req2);
                if (resp.IsSuccessStatusCode)
                {
                    var result = await resp.Content.ReadFromJsonAsync<AgentChatResponse>();
                    return result?.Answer ?? FallbackResponse(userMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent service unavailable, using fallback response");
            }
        }

        return FallbackResponse(userMessage);
    }

    private static string FallbackResponse(string userMessage)
    {
        var lower = userMessage.ToLower();
        if (lower.Contains("bảo hiểm") || lower.Contains("bhyt"))
            return "Tôi có thể giúp bạn tư vấn về bảo hiểm y tế. Bảo hiểm y tế (BHYT) là hình thức bảo hiểm được áp dụng nhằm chi trả chi phí khám chữa bệnh. Bạn muốn biết thêm về quyền lợi, mức đóng hay thủ tục khám chữa bệnh BHYT?";
        if (lower.Contains("đặt lịch") || lower.Contains("khám bệnh"))
            return "Để đặt lịch khám, bạn có thể vào mục 'Đăng ký khám' trên menu hoặc tôi có thể hướng dẫn bạn từng bước. Bạn muốn đặt lịch khám khoa nào?";
        if (lower.Contains("xin chào") || lower.Contains("hello") || lower.Contains("hi"))
            return "Xin chào! Tôi là trợ lý ảo của MGSPlus. Tôi có thể giúp bạn tư vấn y tế, giải đáp thắc mắc về bảo hiểm y tế, hỗ trợ đặt lịch khám và đọc hồ sơ y tế. Bạn cần hỗ trợ gì?";
        return "Cảm ơn bạn đã liên hệ. Tôi đang xử lý câu hỏi của bạn. Hiện tại hệ thống AI đang trong quá trình khởi động — vui lòng thử lại sau ít phút hoặc liên hệ đường dây hỗ trợ 1800-xxxx.";
    }

    private record AgentChatResponse(string Answer, string ThreadId, string Agent);
}
