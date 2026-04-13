using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class MedicalRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? DoctorId { get; set; }
    public int? AppointmentId { get; set; }

    [MaxLength(500)]
    public string? Diagnosis { get; set; }

    [MaxLength(2000)]
    public string? Prescription { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? LabResults { get; set; }

    [MaxLength(512)]
    public string? AttachmentUrl { get; set; }

    public DateTime RecordDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Doctor? Doctor { get; set; }
}
