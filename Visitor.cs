using System;
using CEMETRIX.Domain.Common;

namespace CEMETRIX.Domain.Entities;

public class Visitor : BaseEntity
{
    public string VisitorName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; } = DateTime.UtcNow;
    public string Purpose { get; set; } = string.Empty;

    public int? GraveId { get; set; }
    public Grave? GraveVisited { get; set; }

    public string? ContactInfo { get; set; }
    public string? Notes { get; set; }
    public DateTime? CheckOutTime { get; set; }
}
