using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class Appointment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? DoctorId { get; set; }

    public DateTime ScheduledAt { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Pending"; // Pending | Confirmed | Cancelled | Completed

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public int? QueueNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Doctor? Doctor { get; set; }
}
