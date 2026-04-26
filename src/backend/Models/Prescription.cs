using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class Prescription
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [MaxLength(500)]
    public string OriginalFileName { get; set; } = string.Empty;

    // OCR extracted text (raw)
    public string? RawOcrText { get; set; }

    // JSON array of extracted medications
    public string? MedicationsJson { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending | Processed | Failed

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
