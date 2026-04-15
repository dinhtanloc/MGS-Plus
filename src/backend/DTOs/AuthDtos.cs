using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    string? PhoneNumber
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string Token,
    string TokenType,
    int ExpiresIn,
    UserDto User,
    string? RefreshToken = null
);

public record RefreshTokenRequest(
    [Required] string RefreshToken
);

public record UserDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Role,
    DateTime CreatedAt
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword
);

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? Address,
    string? InsuranceNumber,
    string? InsuranceProvider,
    string? BloodType,
    string? Allergies,
    string? ChronicDiseases
);
