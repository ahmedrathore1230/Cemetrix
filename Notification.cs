using System;
using CEMETRIX.Domain.Common;
using CEMETRIX.Domain.Enums;

namespace CEMETRIX.Domain.Entities;

public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string? Icon { get; set; }
    public string? Link { get; set; }
    public string? UserId { get; set; }
}
