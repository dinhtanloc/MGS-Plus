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
            <!DOCTYPE html>
            <html lang="vi">
            <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width,initial-scale=1" /></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:40px 16px;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08);">
                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,#2563eb 0%,#1d4ed8 100%);padding:32px 40px;text-align:center;">
                        <span style="display:inline-flex;align-items:center;gap:10px;text-decoration:none;">
                          <span style="width:36px;height:36px;background:rgba(255,255,255,.2);border-radius:10px;display:inline-flex;align-items:center;justify-content:center;">
                            &#x2665;
                          </span>
                          <span style="color:#ffffff;font-size:22px;font-weight:700;letter-spacing:-.5px;">MGSPlus</span>
                        </span>
                        <p style="color:rgba(255,255,255,.8);margin:8px 0 0;font-size:13px;">Hệ thống quản lý bệnh viện thông minh</p>
                      </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                      <td style="padding:40px 40px 32px;">
                        <h1 style="margin:0 0 8px;font-size:22px;font-weight:700;color:#111827;">Xác thực địa chỉ email</h1>
                        <p style="margin:0 0 24px;color:#6b7280;font-size:15px;line-height:1.6;">
                          Xin chào <strong style="color:#111827;">{user.FirstName}</strong>,<br/>
                          Cảm ơn bạn đã đăng ký tài khoản tại MGSPlus. Vui lòng nhấn nút bên dưới để xác thực địa chỉ email của bạn.
                        </p>
                        <div style="text-align:center;margin:32px 0;">
                          <a href="{link}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;font-size:15px;font-weight:600;padding:14px 36px;border-radius:10px;letter-spacing:.2px;">
                            &#x2714; Xác thực email
                          </a>
                        </div>
                        <p style="margin:24px 0 0;font-size:13px;color:#9ca3af;text-align:center;">
                          Hoặc sao chép liên kết sau vào trình duyệt:<br/>
                          <a href="{link}" style="color:#2563eb;word-break:break-all;font-size:12px;">{link}</a>
                        </p>
                      </td>
                    </tr>
                    <!-- Notice -->
                    <tr>
                      <td style="padding:0 40px 32px;">
                        <div style="background:#fef9c3;border:1px solid #fde047;border-radius:10px;padding:14px 18px;">
                          <p style="margin:0;font-size:13px;color:#713f12;">
                            &#x26A0; Liên kết có hiệu lực trong <strong>24 giờ</strong>. Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email này.
                          </p>
                        </div>
                      </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                      <td style="background:#f9fafb;border-top:1px solid #e5e7eb;padding:20px 40px;text-align:center;">
                        <p style="margin:0;font-size:12px;color:#9ca3af;">
                          &copy; 2025 MGSPlus · Hệ thống quản lý bệnh viện thông minh<br/>
                          Email này được gửi tự động, vui lòng không trả lời.
                        </p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
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
