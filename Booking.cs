using System;
using CEMETRIX.Domain.Common;
using CEMETRIX.Domain.Enums;

namespace CEMETRIX.Domain.Entities;

public class Booking : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int GraveId { get; set; }
    public Grave? Grave { get; set; }

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
}
