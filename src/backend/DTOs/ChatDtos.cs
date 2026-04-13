using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.DTOs;

public record CreateChatSessionRequest(
    string? Title,
    string SessionType = "General"
);

public record SendMessageRequest(
    [Required] string Content,
    string? ContextType  // medical_record | insurance | appointment
);

public record ChatSessionDto(
    int Id,
    string? Title,
    string SessionType,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ChatMessageDto(
    int Id,
    string Role,
    string Content,
    DateTime CreatedAt
);

public record ChatResponseDto(
    ChatMessageDto UserMessage,
    ChatMessageDto AssistantMessage
);
