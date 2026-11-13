// Data/WeatherDataSource.cs
using System.Collections.Generic;
namespace AsciiFarm.Data;

public sealed class WeatherDataSource
{
    public List<WeatherData> HourlyData { get; set; } = new();
    public string? LoadError { get; set; }
    public int StartIndex { get; set; } = 0;  // НОВОЕ: индекс первой записи, соответствующей GameStartDate
}