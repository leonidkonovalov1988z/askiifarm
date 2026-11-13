// Data/CalendarHelper.cs
using System;
namespace AsciiFarm.Data;

public static class CalendarHelper
{
    /// <summary>
    /// Количество дней в году для настраиваемого календаря.
    /// </summary>
    public static int GetDaysInYear(int daysPerMonth)
    {
        if (daysPerMonth == 0) return 365; // реальный календарь
        return daysPerMonth * 12;
    }

    /// <summary>
    /// Преобразует реальную дату в дату настраиваемого календаря.
    /// </summary>
    public static DateTime ConvertToCustomCalendar(DateTime realDate, int daysPerMonth)
    {
        if (daysPerMonth == 0) return realDate;

        int year = realDate.Year;
        int dayOfYear = realDate.DayOfYear;
        int month = (dayOfYear - 1) / daysPerMonth + 1;
        int day = (dayOfYear - 1) % daysPerMonth + 1;

        return new DateTime(year, Math.Min(month, 12), Math.Min(day, daysPerMonth),
            realDate.Hour, realDate.Minute, realDate.Second);
    }

    /// <summary>
    /// Преобразует дату настраиваемого календаря в реальную дату.
    /// </summary>
    public static DateTime ConvertFromCustomCalendar(DateTime customDate, int daysPerMonth)
    {
        if (daysPerMonth == 0) return customDate;

        int year = customDate.Year;
        int month = customDate.Month;
        int day = customDate.Day;
        int dayOfYear = (month - 1) * daysPerMonth + day;

        return new DateTime(year, 1, 1).AddDays(dayOfYear - 1)
            .AddHours(customDate.Hour)
            .AddMinutes(customDate.Minute)
            .AddSeconds(customDate.Second);
    }
}