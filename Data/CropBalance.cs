// Data/CropBalance.cs
namespace AsciiFarm.Data;

/// <summary>Скорости урона — за ОДИН РЕАЛЬНЫЙ час; в системе умножаются на TIME_SCALE.</summary>
public sealed class CropBalance
{
    public const double RealHoursPerYear = 8760.0;

    public bool ScaleToGameYear { get; set; } = true;
    public bool UseTileMoisture { get; set; } = false;   // true: влага тайла, false: W.soil_moisture
    public float StartingSeedUnits { get; set; } = 5f;

    public float ColdDamageRate { get; set; } = 0.003f;
    public float FrostDamageRate { get; set; } = 0.008f;
    public float HeatDamageRate { get; set; } = 0.005f;
    public float WaterDamageRate { get; set; } = 0.012f;
    public float DiseaseAccumRate { get; set; } = 0.06f;
    public float DiseaseDamageRate { get; set; } = 0.015f;

    // Эталон «нормальной почвы» — те же значения, что в миграции карты.
    public float ReferenceN { get; set; } = 0.50f;
    public float ReferenceP { get; set; } = 0.30f;
    public float ReferenceK { get; set; } = 0.40f;
    public float MinNutrientFactor { get; set; } = 0.20f;
    public float MaxNutrientFactor { get; set; } = 1.20f;

    public float MaxYieldMultiplier { get; set; } = 1.2f;

    public double TimeScale(int daysPerMonth)
    {
        if (!ScaleToGameYear) return 1.0;
        double gameHours = CalendarHelper.GetDaysInYear(daysPerMonth) * 24.0;
        return gameHours > 0 ? RealHoursPerYear / gameHours : 1.0;
    }
}