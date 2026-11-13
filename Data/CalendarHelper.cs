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
        if (daysPerMonth == 0) return 365;
        return daysPerMonth * 12;
    }

    /// <summary>
    /// Форматирует игровое время с учётом кастомного календаря.
    /// </summary>
    // Data/CalendarHelper.cs
    // Data/CalendarHelper.cs
    public static string FormatGameTime(DateTime startDate, double gameSeconds, int daysPerMonth)
    {
        if (daysPerMonth == 0)
        {
            return startDate.AddSeconds(gameSeconds).ToString("dd.MM.yyyy HH:mm:ss");
        }

        int daysPerYear = daysPerMonth * 12;

        // Пересчитываем начальную дату в координаты кастомного календаря
        // startDate.Month и startDate.Day — из реального календаря
        // Нам нужно преобразовать их в день года кастомного календаря
        // Для простоты: считаем, что 1 мая реального = 1 мая кастомного (тот же номер месяца и дня)
        int startMonth = startDate.Month;
        int startDay = startDate.Day;

        // Ограничиваем в пределах кастомного календаря
        if (startMonth > 12) startMonth = 12;
        if (startDay > daysPerMonth) startDay = daysPerMonth;

        int startDayOfYear = (startMonth - 1) * daysPerMonth + (startDay - 1); // 0-based
        int startHour = startDate.Hour;
        int startMinute = startDate.Minute;
        int startSecond = startDate.Second;

        // Общее время в секундах с начала кастомного года
        long totalSeconds = (long)gameSeconds + (startDayOfYear * 86400) + (startHour * 3600) + (startMinute * 60) + startSecond;

        long totalDays = totalSeconds / 86400;
        long remainingSeconds = totalSeconds % 86400;

        long years = totalDays / daysPerYear;
        long remainingDays = totalDays % daysPerYear;
        int month = (int)(remainingDays / daysPerMonth) + 1;
        int day = (int)(remainingDays % daysPerMonth) + 1;
        int hour = (int)(remainingSeconds / 3600);
        int minute = (int)((remainingSeconds % 3600) / 60);
        int second = (int)(remainingSeconds % 60);

        int year = startDate.Year + (int)years;
        return $"{day:D2}.{month:D2}.{year:D4} {hour:D2}:{minute:D2}:{second:D2}";
    }
}