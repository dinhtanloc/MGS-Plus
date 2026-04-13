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

        // Build AI response (stub — connects to agent service in production)
        var assistantContent = await GenerateResponseAsync(req.Content, req.ContextType, session.Messages);

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

    private async Task<string> GenerateResponseAsync(string userMessage, string? contextType, IEnumerable<ChatMessage> history)
    {
        // Forward to agent service if configured
        var agentUrl = _config["AgentService:SupervisorUrl"];
        if (!string.IsNullOrEmpty(agentUrl))
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var payload = new
                {
                    message = userMessage,
                    context_type = contextType,
                    history = history.Select(m => new { role = m.Role, content = m.Content })
                };
                var resp = await http.PostAsJsonAsync($"{agentUrl}/chat", payload);
                if (resp.IsSuccessStatusCode)
                {
                    var result = await resp.Content.ReadFromJsonAsync<AgentResponse>();
                    return result?.Response ?? FallbackResponse(userMessage);
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

    private record AgentResponse(string Response);
}
