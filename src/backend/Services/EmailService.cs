using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MGSPlus.Api.Models;

namespace MGSPlus.Api.Services;

/// <summary>
/// Sends transactional emails (verification, password reset) via SMTP/MailKit.
/// Configuration keys (from .env):
///   SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, SMTP_FROM, APP_BASE_URL
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── Verification email ─────────────────────────────────────────────────────

    public async Task SendVerificationEmailAsync(User user)
    {
        var token   = GenerateVerificationToken(user.Id);
        var baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5000";
        var link    = $"{baseUrl}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";

        var body = $"""
            <p>Xin chào <strong>{user.FirstName}</strong>,</p>
            <p>Vui lòng nhấn vào liên kết bên dưới để xác thực địa chỉ email của bạn:</p>
            <p><a href="{link}" style="color:#2563eb">Xác thực email</a></p>
            <p>Liên kết có hiệu lực trong 24 giờ.</p>
            <p>Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email này.</p>
            """;

        await SendAsync(user.Email, "MGSPlus — Xác thực địa chỉ email", body);
    }

    /// <summary>
    /// Validate a verification token.
    /// Returns the user ID if valid, null otherwise.
    /// </summary>
    public int? ValidateVerificationToken(string token)
    {
        try
        {
            var secret  = GetHmacSecret();
            var parts   = token.Split('.');
            if (parts.Length != 3) return null;

            var userId    = int.Parse(parts[0]);
            var expiresTick = long.Parse(parts[1]);
            var sig       = parts[2];

            if (DateTime.UtcNow.Ticks > expiresTick) return null;

            var expected = ComputeHmac(secret, $"{userId}.{expiresTick}");
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(sig),
                    Encoding.UTF8.GetBytes(expected)))
                return null;

            return userId;
        }
        catch
        {
            return null;
        }
    }

    // ── SMTP send ──────────────────────────────────────────────────────────────

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("SMTP not configured — skipping email to {To}: {Subject}", to, subject);
            return;
        }

        var port    = int.Parse(_config["Smtp:Port"] ?? "587");
        var user    = _config["Smtp:User"] ?? "";
        var pass    = _config["Smtp:Pass"] ?? "";
        var from    = _config["Smtp:From"] ?? user;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = htmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable);
        if (!string.IsNullOrEmpty(user))
            await smtp.AuthenticateAsync(user, pass);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    // ── Token helpers ──────────────────────────────────────────────────────────

    private string GenerateVerificationToken(int userId)
    {
        var secret  = GetHmacSecret();
        var expires = DateTime.UtcNow.AddHours(24).Ticks;
        var sig     = ComputeHmac(secret, $"{userId}.{expires}");
        return $"{userId}.{expires}.{sig}";
    }

    private string GetHmacSecret() =>
        _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");

    private static string ComputeHmac(string secret, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }
}
