using System.Globalization;

namespace Lingual.Modules.Gamification.Common;

public static class TimeZoneHelper
{
    private static readonly Lazy<TimeZoneInfo> VietnamTimeZoneLazy = new(() =>
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Windows
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); // Linux / IANA
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "ICT", "ICT");
            }
        }
    });

    public static TimeZoneInfo VietnamTimeZone => VietnamTimeZoneLazy.Value;

    public static DateOnly GetCurrentVietnamDate()
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        return DateOnly.FromDateTime(localNow);
    }

    public static string GetCurrentWeekKey()
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        var cal = CultureInfo.InvariantCulture.Calendar;
        int week = cal.GetWeekOfYear(localNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{localNow.Year}-W{week:D2}";
    }

    public static (DateOnly Start, DateOnly End) GetCurrentWeekRange()
    {
        var today = GetCurrentVietnamDate();
        int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var start = today.AddDays(-diff);
        var end = start.AddDays(6);
        return (start, end);
    }
}

