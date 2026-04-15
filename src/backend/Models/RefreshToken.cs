using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class RefreshToken
{
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Token { get; set; } = string.Empty;   // SHA-256 hashed

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ExpiresAt  { get; set; }
    public bool     IsRevoked  { get; set; } = false;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}
