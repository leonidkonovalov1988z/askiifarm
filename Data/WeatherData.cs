// Data/WeatherData.cs
namespace AsciiFarm.Data;

/// <summary>
/// Одна запись погоды после всех трансформаций.
/// </summary>
public readonly struct WeatherData
{
    public DateTime Timestamp { get; init; }  // ← НОВОЕ: время из CSV

    public float AirTemp { get; init; }          // °C
    public float GroundTemp { get; init; }       // °C, с учётом снежного одеяла
    public float AirHumidity { get; init; }      // 0.0–1.0
    public float SoilMoisture { get; init; }     // 0.0–1.0
    public float SolarEnergyMj { get; init; }    // МДж/м² за час
    public float EffectiveSnowCm { get; init; }  // см, с коррекцией
    public float PrecipitationMm { get; init; }  // мм за час
    public float GroundTempMin { get; init; }  // НОВОЕ
    public float GroundTempMax { get; init; }  // НОВОЕ
    public float AirTempMin { get; init; }  // НОВОЕ
    public float AirTempMax { get; init; }  // НОВОЕ
}