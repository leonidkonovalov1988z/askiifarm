// Data/WeatherSystemState.cs
namespace AsciiFarm.Data;

/// <summary>Состояние WeatherSystem, вынесено из системы в ресурс мира.</summary>
public sealed class WeatherSystemState
{
    public int LastWeatherIndex { get; set; } = -1;
}