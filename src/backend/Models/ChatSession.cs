using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class ChatSession
{
    public int Id { get; set; }
    public int? UserId { get; set; }

    [MaxLength(100)]
    public string? Title { get; set; }

    [MaxLength(30)]
    public string SessionType { get; set; } = "General"; // General | MedicalRecord | Insurance | Appointment

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }

    [MaxLength(20)]
    public string Role { get; set; } = "user"; // user | assistant | system

    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Model { get; set; }

    public int? TokensUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ChatSession Session { get; set; } = null!;
}
