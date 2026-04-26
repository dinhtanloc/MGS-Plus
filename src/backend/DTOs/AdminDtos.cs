using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.DTOs;

public record AdminStatsDto(
    int TotalUsers,
    int TotalDoctors,
    int TotalPatients,
    int PendingDoctors,
    int TotalAppointments,
    int TotalBlogPosts,
    int TotalNews,
    int AppointmentsThisMonth,
    IReadOnlyList<MonthlyStatDto> AppointmentsByMonth
);

public record MonthlyStatDto(int Year, int Month, int Count);

public record DoctorApplicationDto(
    int DoctorId,
    int UserId,
    string FullName,
    string Email,
    string Specialty,
    string LicenseNumber,
    string? Bio,
    decimal ConsultationFee,
    string ApplicationStatus,
    string? RejectionReason,
    DateTime CreatedAt
);

public record ReviewDoctorApplicationRequest(
    [Required] string Action,   // "approve" | "reject"
    string? RejectionReason
);

public record GrantAdminRequest(
    [Required] int TargetUserId
);

public record AdminUserDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    bool IsEmailVerified,
    DateTime CreatedAt
);
