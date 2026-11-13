// Content/WeatherCsvLoader.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AsciiFarm.Data;
namespace AsciiFarm.Content;

/// <summary>
/// Парсит CSV погоды. Поддерживает два формата:
///   1) Плоский: заголовки в 1-й строке, имена могут содержать единицы
///      ("temperature_2m (°C)").
///   2) Open-Meteo API: заголовки в строке "hourly", затем "hourly_units", затем данные.
/// </summary>
public static class WeatherCsvLoader
{
    private const float WiltingPoint = 0.10f;
    private const float FieldCapacity = 0.38f;
    private const float SnowCorrectionFactor = 0.5f;
    private const float SnowInsulationFactor = 0.15f;
    private const float SnowMeltTempThreshold = 3f;

    public static List<WeatherData> Load(string filePath)
        => Load(filePath, out _);

    // Content/WeatherCsvLoader.cs — заменить метод Load
    public static List<WeatherData> Load(string filePath, out string? error)
    {
        error = null;
        var result = new List<WeatherData>();
        if (!File.Exists(filePath)) { error = $"Файл не найден: {filePath}"; return result; }

        string[] lines;
        try { lines = File.ReadAllLines(filePath); }
        catch (Exception ex) { error = $"Не удалось прочитать: {ex.Message}"; return result; }

        if (lines.Length < 2) { error = "Файл слишком короткий"; return result; }

        int headerIndex = -1;
        for (int i = 0; i < Math.Min(10, lines.Length); i++)
        {
            var lower = lines[i].ToLowerInvariant();
            if (lower.Contains("time") && lower.Contains("temperature"))
            { headerIndex = i; break; }
        }
        if (headerIndex < 0) { error = "Не найдена строка заголовков"; return result; }

        var headers = ParseCsvLine(lines[headerIndex]).Select(NormalizeHeader).ToList();
        int idxTime = headers.IndexOf("time");
        int idxTemp = headers.IndexOf("temperature_2m");
        int idxHum = headers.IndexOf("relative_humidity_2m");
        int idxRad = headers.IndexOf("shortwave_radiation");
        int idxSoil = headers.IndexOf("soil_moisture_7_to_28cm");
        int idxSnow = headers.IndexOf("snow_depth");
        int idxPrecip = headers.IndexOf("precipitation");

        if (idxTemp < 0) { error = $"Нет столбца temperature_2m"; return result; }

        for (int i = headerIndex + 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var fields = ParseCsvLine(lines[i]);
            if (fields.Count == 0 || !LooksLikeDate(fields[0])) continue;
            if (fields.Count <= idxTemp) continue;

            // Парсим время
            DateTime timestamp = DateTime.MinValue;
            if (idxTime >= 0 && idxTime < fields.Count)
                DateTime.TryParse(fields[idxTime], CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out timestamp);

            float temp = ParseFloat(fields, idxTemp);
            float hum = ParseFloat(fields, idxHum);
            float rad = ParseFloat(fields, idxRad);
            float soil = ParseFloat(fields, idxSoil);
            float snow = ParseFloat(fields, idxSnow);
            float precip = idxPrecip >= 0 ? ParseFloat(fields, idxPrecip) : 0f;

            float solarEnergyMj = rad * 0.0036f;
            float soilMoisture = Math.Clamp((soil - 0.10f) / 0.28f, 0f, 1f);
            float effectiveSnowCm = (snow * 100f) * 0.5f;
            if (temp > 3f) effectiveSnowCm = 0f;
            float groundTemp = (temp < 0f && effectiveSnowCm > 0f)
                ? temp / (1f + 0.15f * effectiveSnowCm) : temp;
            float airHumidity = Math.Clamp(hum / 100f, 0f, 1f);

            result.Add(new WeatherData
            {
                Timestamp = timestamp,
                AirTemp = temp,
                GroundTemp = groundTemp,
                AirHumidity = airHumidity,
                SoilMoisture = soilMoisture,
                SolarEnergyMj = solarEnergyMj,
                EffectiveSnowCm = effectiveSnowCm,
                PrecipitationMm = precip
            });
        }

        if (result.Count == 0) error = "Ни одна строка данных не распознана";
        return result;
    }

    // "temperature_2m (°C)" → "temperature_2m"
    // '"hourly"'            → "hourly"
    private static string NormalizeHeader(string raw)
    {
        var s = raw.Trim().Trim('"');
        int paren = s.IndexOf('(');
        if (paren >= 0) s = s.Substring(0, paren);
        return s.Trim().ToLowerInvariant();
    }

    // "2000-01-01T00:00" → true;  "iso8601" → false
    private static bool LooksLikeDate(string first)
    {
        if (string.IsNullOrEmpty(first)) return false;
        char c0 = first[0];
        return c0 >= '0' && c0 <= '9';
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result;
    }

    private static float ParseFloat(List<string> fields, int index)
    {
        if (index < 0 || index >= fields.Count) return 0f;
        return float.TryParse(fields[index], NumberStyles.Float,
            CultureInfo.InvariantCulture, out var v) ? v : 0f;
    }
    // Content/WeatherCsvLoader.cs — добавить метод
    public static int FindStartIndex(List<WeatherData> data, DateTime startDate)
    {
        // Находим первую запись, которая >= startDate
        for (int i = 0; i < data.Count; i++)
        {
            if (data[i].Timestamp >= startDate)
                return i;
        }
        // Если не нашли, начинаем с 0
        return 0;
    }
    // Content/WeatherCsvLoader.cs — добавить метод
    public static List<WeatherData> CompressWeatherData(List<WeatherData> original, int daysPerMonth)
    {
        if (daysPerMonth == 0 || original.Count == 0) return original;

        int originalDays = 365; // предполагаем год данных
        int newDays = CalendarHelper.GetDaysInYear(daysPerMonth);
        double compressionRatio = (double)originalDays / newDays;

        var result = new List<WeatherData>();

        for (int i = 0; i < newDays; i++)
        {
            int startIdx = (int)(i * compressionRatio);
            int endIdx = (int)((i + 1) * compressionRatio);
            endIdx = Math.Min(endIdx, original.Count);

            if (startIdx >= original.Count) break;

            var range = original.Skip(startIdx).Take(endIdx - startIdx).ToList();

            // Усредняем данные с сохранением min/max
            var compressed = new WeatherData
            {
                Timestamp = range.First().Timestamp,
                AirTemp = range.Average(d => d.AirTemp),
                AirTempMin = range.Min(d => d.AirTemp),
                AirTempMax = range.Max(d => d.AirTemp),
                GroundTemp = range.Average(d => d.GroundTemp),
                GroundTempMin = range.Min(d => d.GroundTemp),
                GroundTempMax = range.Max(d => d.GroundTemp),
                AirHumidity = range.Average(d => d.AirHumidity),
                SoilMoisture = range.Average(d => d.SoilMoisture),
                SolarEnergyMj = range.Average(d => d.SolarEnergyMj),
                EffectiveSnowCm = range.Average(d => d.EffectiveSnowCm),
                PrecipitationMm = range.Sum(d => d.PrecipitationMm)  // осадки суммируются
            };

            result.Add(compressed);
        }

        return result;
    }
}