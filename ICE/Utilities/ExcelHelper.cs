using Lumina.Excel;
using Lumina.Excel.Sheets;

internal static class ExcelHelper
{
    internal static ExcelSheet<Item>? ItemSheet;
    internal static ExcelSheet<Recipe>? RecipeSheet;
    internal static ExcelSheet<Weather>? WeatherSheet;
    internal static ExcelSheet<WKSDevGrade>? DevGrade;

    public static void Init() // Only need to grab once, it won't change
    {
        ItemSheet ??= Svc.Data.GetExcelSheet<Item>();
        RecipeSheet ??= Svc.Data.GetExcelSheet<Recipe>();
        WeatherSheet ??= Svc.Data.GetExcelSheet<Weather>();
        DevGrade ??= Svc.Data.GetExcelSheet<WKSDevGrade>();
    }
}