using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class Doctor
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [MaxLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Bio { get; set; }

    public decimal ConsultationFee { get; set; }

    [MaxLength(200)]
    public string? ClinicAddress { get; set; }

    public double Rating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;
    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
