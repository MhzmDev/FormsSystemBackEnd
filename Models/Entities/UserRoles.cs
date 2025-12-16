namespace DynamicForm.Models.Entities;

public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin"; // ✅ NEW
    public const string Employee = "Employee";
    public static readonly string[] All = { SuperAdmin, Admin, Employee };

    public static bool IsValid(string role)
    {
        return All.Contains(role);
    }
}