using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class DoctorSchedule
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    /// <summary>0 = Sunday, 1 = Monday … 6 = Saturday</summary>
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime   { get; set; }

    public bool IsAvailable { get; set; } = true;
}
