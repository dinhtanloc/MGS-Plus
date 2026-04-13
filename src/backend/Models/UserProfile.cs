using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class UserProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? InsuranceNumber { get; set; }

    [MaxLength(100)]
    public string? InsuranceProvider { get; set; }

    public DateTime? InsuranceStartDate { get; set; }
    public DateTime? InsuranceEndDate { get; set; }

    [MaxLength(10)]
    public string? BloodType { get; set; }

    [MaxLength(500)]
    public string? Allergies { get; set; }

    [MaxLength(1000)]
    public string? ChronicDiseases { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
