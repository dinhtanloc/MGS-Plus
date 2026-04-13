using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.DTOs;

public record CreateAppointmentRequest(
    [Required] DateTime ScheduledAt,
    int? DoctorId,
    string? Department,
    string? Description
);

public record UpdateAppointmentRequest(
    DateTime? ScheduledAt,
    string? Status,
    string? Notes,
    string? Department
);

public record AppointmentDto(
    int Id,
    int UserId,
    string UserName,
    int? DoctorId,
    string? DoctorName,
    string? DoctorSpecialty,
    DateTime ScheduledAt,
    string Status,
    string? Description,
    string? Notes,
    string? Department,
    int? QueueNumber,
    DateTime CreatedAt
);
