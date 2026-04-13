using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string Role { get; set; } = "Patient";  // Patient | Doctor | Admin

    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public UserProfile? Profile { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
