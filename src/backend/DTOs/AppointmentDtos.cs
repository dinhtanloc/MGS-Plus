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
    string? UserEmail,
    string? UserPhone,
    int? DoctorId,
    string? DoctorName,
    string? DoctorSpecialty,
    DateTime ScheduledAt,
    string Status,
    string? Description,
    string? Notes,
    string? Department,
    int? QueueNumber,
    string? RescheduleReason,
    DateTime? RescheduledTo,
    string? CancelReason,
    DateTime CreatedAt
);

public record DoctorAppointmentActionRequest(
    [Required] string Action,   // "accept" | "reschedule" | "reject"
    string? Reason,
    DateTime? RescheduledTo
);

public record SubmitReviewRequest(
    [Required] int AppointmentId,
    [Required, Range(1, 5)] int Rating,
    [MaxLength(1000)] string? Comment
);
