using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class DirectChatSession
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active | Closed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ICollection<DirectMessage> Messages { get; set; } = new List<DirectMessage>();
}

public class DirectMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int SenderId { get; set; }

    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public DirectChatSession Session { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
