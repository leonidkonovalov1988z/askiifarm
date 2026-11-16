// Data/CropBalance.cs
namespace AsciiFarm.Data;

/// <summary>Скорости урона за ОДИН РЕАЛЬНЫЙ час; в системе умножаются на TIME_SCALE.</summary>
public sealed class CropBalance
{
    public const double RealHoursPerYear = 8760.0;

    public bool ScaleToGameYear { get; set; } = true;   // способ 1; false = способ 2 (CSV уже масштабирован)
    public bool UseTileMoisture { get; set; } = true;   // true: TileData.Moisture, false: W.soil_moisture
    public float StartingSeedUnits { get; set; } = 5f;

    public float ColdDamageRate { get; set; } = 0.003f;
    public float FrostDamageRate { get; set; } = 0.008f;
    public float HeatDamageRate { get; set; } = 0.005f;
    public float WaterDamageRate { get; set; } = 0.012f;
    public float DiseaseAccumRate { get; set; } = 0.06f;
    public float DiseaseDamageRate { get; set; } = 0.015f;

    public float MinFertility { get; set; } = 0.2f;
    public float MaxFertility { get; set; } = 1.0f;
    public float MaxYieldMultiplier { get; set; } = 1.2f;

    public double TimeScale(int daysPerMonth)
    {
        if (!ScaleToGameYear) return 1.0;
        double gameHours = CalendarHelper.GetDaysInYear(daysPerMonth) * 24.0;
        return gameHours > 0 ? RealHoursPerYear / gameHours : 1.0;
    }
}