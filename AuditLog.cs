using System;

namespace CEMETRIX.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Changes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? PerformedBy { get; set; }
}
