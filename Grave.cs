using System;
using System.Collections.Generic;
using CEMETRIX.Domain.Common;
using CEMETRIX.Domain.Enums;

namespace CEMETRIX.Domain.Entities;

public class Grave : BaseEntity
{
    public string GraveNumber { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
    public GraveStatus Status { get; set; } = GraveStatus.Available;
    public DateTime? BurialDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Notes { get; set; }
    public bool IsReusable { get; set; } = true;
    public decimal Price { get; set; }

    public ICollection<DeceasedPerson> DeceasedPersons { get; set; } = new List<DeceasedPerson>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Visitor> Visitors { get; set; } = new List<Visitor>();
}
