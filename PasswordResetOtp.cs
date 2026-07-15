namespace CEMETRIX.Domain.Entities;

/// <summary>One-time code for password reset or change, stored hashed in SQL Server.</summary>
public class PasswordResetOtp
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    /// <summary>ResetPassword or ChangePassword</summary>
    public string Purpose { get; set; } = "ResetPassword";
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }
}
