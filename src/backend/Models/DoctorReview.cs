using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class DoctorReview
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int UserId { get; set; }
    public int AppointmentId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Doctor Doctor { get; set; } = null!;
    public User User { get; set; } = null!;
    public Appointment Appointment { get; set; } = null!;
}
