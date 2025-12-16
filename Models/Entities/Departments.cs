namespace DynamicForm.Models.Entities;

public static class Departments
{
    public const string Sales = "Sales";
    public const string Marketing = "Marketing";
    public const string SalesAr = "مبيعات";
    public const string MarketingAr = "تسويق";

    public static readonly Dictionary<string, string> NameMap = new()
    {
        { Sales, SalesAr },
        { Marketing, MarketingAr }
    };

    public static readonly string[] All = { Sales, Marketing };

    public static bool IsValid(string department)
    {
        return All.Contains(department);
    }

    public static string GetArabicName(string department)
    {
        return NameMap.TryGetValue(department, out var arabicName) ? arabicName : department;
    }

    public static string? GetEnglishName(string arabicName)
    {
        return NameMap.FirstOrDefault(x => x.Value == arabicName).Key;
    }
}