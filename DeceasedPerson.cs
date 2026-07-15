using System;
using CEMETRIX.Domain.Common;
using CEMETRIX.Domain.Enums;

namespace CEMETRIX.Domain.Entities;

public class DeceasedPerson : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public DateTime DOB { get; set; }
    public DateTime DOD { get; set; }
    public Gender Gender { get; set; }
    public string Religion { get; set; } = string.Empty;
    public BurialType BurialType { get; set; }
    public string? PhotoUrl { get; set; }
    public string? FamilyContact { get; set; }
    public string? MemorialNotes { get; set; }
    public string? Nationality { get; set; }
    public string? CauseOfDeath { get; set; }

    public int GraveId { get; set; }
    public Grave? Grave { get; set; }

    public int Age => Math.Max(0, (int)((DOD - DOB).TotalDays / 365.25));
}
