// Data/WeatherCurrent.cs
namespace AsciiFarm.Data;

/// <summary>Текущая погода — ресурс, обновляется WeatherSystem.</summary>
public sealed class WeatherCurrent
{
    public WeatherData Current { get; set; }
    public int HourIndex { get; set; }
    public bool HasData { get; set; }
    public DateTime GameTime { get; set; }  // ← НОВОЕ

}