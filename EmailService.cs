using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CEMETRIX.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CEMETRIX.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        if (!_settings.IsSendingEnabled)
        {
            _logger.LogInformation("[Email disabled] To: {To}, Subject: {Subject}", to, subject);
            if (subject.Contains("verification", StringComparison.OrdinalIgnoreCase)
                || subject.Contains("reset code", StringComparison.OrdinalIgnoreCase)
                || subject.Contains("Confirm", StringComparison.OrdinalIgnoreCase))
            {
                var otpMatch = System.Text.RegularExpressions.Regex.Match(htmlBody, @">\d{6}<");
                if (otpMatch.Success)
                    _logger.LogWarning("[DEV] OTP in email body: {Otp}", otpMatch.Value.Trim('>'));
            }
            return;
        }

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpServer, _settings.Port,
            _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        if (!string.IsNullOrWhiteSpace(_settings.Username))
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }

    public Task SendTemplatedEmailAsync(string to, string subject, string templateName, object model)
    {
        var html = RenderTemplate(templateName, model);
        return SendEmailAsync(to, subject, html);
    }

    public Task SendWelcomeEmailAsync(string to, string fullName)
    {
        var html = WrapTemplate("Welcome to CEMETRIX",
            $"<h2>Welcome, {fullName}!</h2><p>Your CEMETRIX account is ready. You can now sign in and start managing your cemetery operations.</p>",
            "Open Dashboard", "/");
        return SendEmailAsync(to, "Welcome to CEMETRIX", html);
    }

    public Task SendPasswordResetEmailAsync(string to, string fullName, string resetLink)
    {
        var url = _settings.BaseUrl.TrimEnd('/') + resetLink;
        var html = WrapTemplate("Reset your password",
            $"<h2>Hello {fullName},</h2><p>We received a request to reset your password. Click the button below to choose a new password. The link is valid for 1 hour.</p>",
            "Reset Password", url);
        return SendEmailAsync(to, "Reset your CEMETRIX password", html);
    }

    public Task SendOtpEmailAsync(string to, string fullName, string otpCode, string purpose)
    {
        var title = purpose == "ChangePassword" ? "Password change verification" : "Password reset code";
        var body = purpose == "ChangePassword"
            ? $"<h2>Hello {fullName},</h2><p>Your verification code to change your CEMETRIX password is:</p><p style='font-size:32px;font-weight:700;letter-spacing:8px;color:#16a34a;'>{otpCode}</p><p>This code expires in <b>15 minutes</b>. If you did not request this, ignore this email.</p>"
            : $"<h2>Hello {fullName},</h2><p>Your password reset code for CEMETRIX is:</p><p style='font-size:32px;font-weight:700;letter-spacing:8px;color:#16a34a;'>{otpCode}</p><p>Enter this code on the reset page. It expires in <b>15 minutes</b>.</p>";
        var html = WrapTemplate(title, body, "Open CEMETRIX", "/login");
        return SendEmailAsync(to, $"CEMETRIX — {title}", html);
    }

    public Task SendEmailConfirmationAsync(string to, string fullName, string confirmLink)
    {
        var url = confirmLink.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? confirmLink
            : _settings.BaseUrl.TrimEnd('/') + confirmLink;
        var html = WrapTemplate("Confirm your email",
            $"<h2>Welcome, {fullName}!</h2><p>Please confirm your email address to activate your CEMETRIX account.</p>",
            "Confirm email", url);
        return SendEmailAsync(to, "Confirm your CEMETRIX email", html);
    }

    public Task SendExpirationAlertAsync(string to, string fullName, string graveNumber, DateTime expiration)
    {
        var html = WrapTemplate("Grave expiration alert",
            $"<h2>Hello {fullName},</h2><p>Grave <b>{graveNumber}</b> is approaching its expiration date on <b>{expiration:dd MMM yyyy}</b>. Please take the appropriate action.</p>",
            "Open in CEMETRIX", "/graveyard-map");
        return SendEmailAsync(to, "Grave expiration alert", html);
    }

    public Task SendBookingConfirmationAsync(string to, string fullName, string graveNumber, decimal amount)
    {
        var html = WrapTemplate("Booking confirmed",
            $"<h2>Thank you, {fullName}!</h2><p>Your booking for grave <b>{graveNumber}</b> has been confirmed.</p><p><b>Amount:</b> ${amount:N2}</p>",
            "View Booking", "/booking");
        return SendEmailAsync(to, "Your CEMETRIX booking is confirmed", html);
    }

    private string RenderTemplate(string templateName, object model)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", templateName + ".html");
        if (!File.Exists(path)) return string.Empty;
        var raw = File.ReadAllText(path, Encoding.UTF8);
        foreach (var prop in model.GetType().GetProperties())
        {
            var value = prop.GetValue(model)?.ToString() ?? string.Empty;
            raw = raw.Replace("{{" + prop.Name + "}}", value);
        }
        return raw;
    }

    private string WrapTemplate(string title, string body, string ctaText, string ctaUrl)
    {
        var url = ctaUrl.StartsWith("http") ? ctaUrl : _settings.BaseUrl.TrimEnd('/') + ctaUrl;
        return $@"<!doctype html>
<html><head><meta charset='utf-8'><title>{title}</title></head>
<body style='font-family:Segoe UI,Roboto,Arial,sans-serif;background:#f4f6fa;margin:0;padding:40px;'>
  <div style='max-width:600px;margin:auto;background:#fff;border-radius:18px;overflow:hidden;box-shadow:0 18px 40px rgba(15,23,42,.08);'>
    <div style='background:linear-gradient(135deg,#16a34a,#0f766e);padding:32px;color:#fff;'>
      <h1 style='margin:0;font-size:24px;letter-spacing:.5px;'>CEMETRIX</h1>
      <p style='margin:6px 0 0;opacity:.9;'>Enterprise Graveyard Management</p>
    </div>
    <div style='padding:36px;color:#1f2937;line-height:1.6;'>
      {body}
      <p style='margin-top:32px;'>
        <a href='{url}' style='background:#16a34a;color:#fff;text-decoration:none;padding:14px 28px;border-radius:12px;display:inline-block;font-weight:600;'>{ctaText}</a>
      </p>
      <p style='font-size:12px;color:#94a3b8;margin-top:36px;'>If you didn't request this email, you can safely ignore it.</p>
    </div>
    <div style='background:#0f172a;color:#94a3b8;text-align:center;padding:16px;font-size:12px;'>
      &copy; {DateTime.UtcNow.Year} CEMETRIX — All rights reserved.
    </div>
  </div>
</body></html>";
    }
}
