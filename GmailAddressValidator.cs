using System;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CEMETRIX.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CEMETRIX.Infrastructure.Email;

public interface IGmailAddressValidator
{
    bool IsGmailAddress(string email);
    Task<(bool Success, string Message)> VerifyDeliverableAsync(string email, CancellationToken cancellationToken = default);
}

/// <summary>Validates Gmail addresses and optionally proves deliverability by sending a welcome message.</summary>
public class GmailAddressValidator : IGmailAddressValidator
{
    private static readonly Regex GmailHost = new(@"^(gmail|googlemail)\.com$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IEmailService _email;
    private readonly EmailSettings _settings;
    private readonly ILogger<GmailAddressValidator> _logger;

    public GmailAddressValidator(IEmailService email, IOptions<EmailSettings> options, ILogger<GmailAddressValidator> logger)
    {
        _email = email;
        _settings = options.Value;
        _logger = logger;
    }

    public bool IsGmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new MailAddress(email.Trim());
            var host = addr.Host;
            return GmailHost.IsMatch(host);
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message)> VerifyDeliverableAsync(string email, CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        if (!IsGmailAddress(email))
            return (false, "Please use a valid Gmail address (example@gmail.com).");

        if (!_settings.IsSendingEnabled)
        {
            _logger.LogInformation("Gmail format accepted for {Email} (SMTP disabled — skipping delivery test).", email);
            return (true, "Gmail address accepted.");
        }

        try
        {
            await _email.SendWelcomeEmailAsync(email, email.Split('@')[0]);
            return (true, "Welcome email sent to your Gmail.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gmail delivery test failed for {Email}", email);
            return (false, "We could not deliver email to this Gmail address. Check the address or try again later.");
        }
    }
}
