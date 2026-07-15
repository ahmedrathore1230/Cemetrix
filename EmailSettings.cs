namespace CEMETRIX.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "CEMETRIX";
    public string FromEmail { get; set; } = "no-reply@cemetrix.local";
    public bool UseSsl { get; set; } = true;
    public bool EnableSending { get; set; } = false;
    public string BaseUrl { get; set; } = "https://localhost:5001";

    /// <summary>True when SMTP credentials are configured and sending is allowed.</summary>
    public bool IsSendingEnabled =>
        EnableSending
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(FromEmail);
}
