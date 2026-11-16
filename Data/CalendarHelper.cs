// Data/CalendarHelper.cs
using System;

namespace AsciiFarm.Data;

public static class CalendarHelper
{
    public const double SecondsPerMinute = 60.0;
    public const double SecondsPerHour = 3600.0;
    public const double SecondsPerDay = 86400.0;
    public const int MonthsPerYear = 12;

    /// <summary>Дата/время игрового календаря.</summary>
    public readonly record struct GameDate(int Year, int Month, int Day, int Hour, int Minute, int Second);

    /// <summary>Дней в игровом году. 0 = реальный календарь (365).</summary>
    public static int GetDaysInYear(int daysPerMonth) =>
        daysPerMonth > 0 ? daysPerMonth * MonthsPerYear : 365;

    /// <summary>Дней в игровом месяце. 0 = реальный календарь.</summary>
    public static int GetDaysInMonth(int year, int month, int daysPerMonth) =>
        daysPerMonth > 0
            ? daysPerMonth
            : DateTime.DaysInMonth(year, Math.Clamp(month, 1, 12));

    /// <summary>Текущая игровая дата с учётом кастомного календаря.</summary>
    public static GameDate GetGameDate(DateTime startDate, double gameSeconds, int daysPerMonth)
    {
        if (daysPerMonth <= 0)
        {
            var real = AddSecondsSafe(startDate, gameSeconds);
            return new GameDate(real.Year, real.Month, real.Day, real.Hour, real.Minute, real.Second);
        }

        int daysPerYear = daysPerMonth * MonthsPerYear;

        int startMonth = Math.Clamp(startDate.Month, 1, 12);
        int startDay = Math.Clamp(startDate.Day, 1, daysPerMonth);
        int startDayOfYear = (startMonth - 1) * daysPerMonth + (startDay - 1);   // 0-based

        long totalSeconds = (long)gameSeconds
                            + startDayOfYear * 86400L
                            + startDate.Hour * 3600L
                            + startDate.Minute * 60L
                            + startDate.Second;

        long totalDays = totalSeconds / 86400L;
        long remSeconds = totalSeconds % 86400L;
        long years = totalDays / daysPerYear;
        long remDays = totalDays % daysPerYear;

        return new GameDate(
            startDate.Year + (int)years,
            (int)(remDays / daysPerMonth) + 1,
            (int)(remDays % daysPerMonth) + 1,
            (int)(remSeconds / 3600),
            (int)(remSeconds % 3600 / 60),
            (int)(remSeconds % 60));
    }

    /// <summary>Длина игрового месяца в секундах (для P+4 / X+4).</summary>
    public static double GetMonthSeconds(DateTime startDate, double gameSeconds, int daysPerMonth)
    {
        if (daysPerMonth > 0) return daysPerMonth * SecondsPerDay;
        var d = GetGameDate(startDate, gameSeconds, daysPerMonth);
        return DateTime.DaysInMonth(d.Year, d.Month) * SecondsPerDay;
    }

    /// <summary>Длина игрового года в секундах: 12 игровых месяцев (для P+5 / X+5).</summary>
    public static double GetYearSeconds(DateTime startDate, double gameSeconds, int daysPerMonth)
    {
        if (daysPerMonth > 0) return daysPerMonth * MonthsPerYear * SecondsPerDay;
        var d = GetGameDate(startDate, gameSeconds, daysPerMonth);
        return (DateTime.IsLeapYear(d.Year) ? 366 : 365) * SecondsPerDay;
    }

    /// <summary>Форматирует игровое время с учётом кастомного календаря.</summary>
    public static string FormatGameTime(DateTime startDate, double gameSeconds, int daysPerMonth)
    {
        var d = GetGameDate(startDate, gameSeconds, daysPerMonth);
        return $"{d.Day:D2}.{d.Month:D2}.{d.Year:D4} {d.Hour:D2}:{d.Minute:D2}:{d.Second:D2}";
    }

    // Защита от выхода за DateTime.MaxValue при длинных промотках в режиме реального календаря.
    private static DateTime AddSecondsSafe(DateTime start, double seconds)
    {
        double max = (DateTime.MaxValue - start).TotalSeconds;
        return start.AddSeconds(Math.Clamp(seconds, 0.0, max));
    }
}