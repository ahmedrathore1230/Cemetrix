namespace CEMETRIX.Domain.Enums;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Viewer = "Viewer";

    public static readonly string[] All = { Admin, Manager, Staff, Viewer };
}
