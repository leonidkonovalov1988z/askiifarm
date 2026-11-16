// Data/CropMath.cs
using AsciiFarm.Components;

namespace AsciiFarm.Data;

public static class CropMath
{
    public static PlantStage StageOf(in PlantComponent p, CropDefinition c)
    {
        if (p.Health <= 0f) return PlantStage.Dead;
        double target = c.TargetGdd(p.IsYear2);
        double flower = Math.Min(c.GddFlower, target);   // у лука seed_year2 < gdd_flower
        double veg = Math.Min(c.GddVeg, flower);

        if (p.GddAccumulated < veg) return PlantStage.Germination;
        if (p.GddAccumulated < flower) return PlantStage.Vegetative;
        if (p.GddAccumulated < target) return PlantStage.Flowering;
        return PlantStage.Ripe;
    }

    /// <summary>Трапеция 0..1: min → optMin → optMax → max. Безопасна при вырожденных порогах.</summary>
    public static float Factor(float value, float min, float optMin, float optMax, float max)
    {
        if (value <= min || value >= max) return 0f;
        optMin = Math.Clamp(optMin, min, max);
        optMax = Math.Clamp(optMax, optMin, max);
        if (value < optMin) return optMin > min ? (value - min) / (optMin - min) : 1f;
        if (value > optMax) return max > optMax ? (max - value) / (max - optMax) : 1f;
        return 1f;
    }

    public static float Yield(in PlantComponent p, CropDefinition c, float cap = 1.2f)
    {
        if (c.SolarMjTarget <= 0) return 0f;
        double raw = Math.Clamp(p.BiomassMj / c.SolarMjTarget, 0.0, cap);
        return (float)(raw * Math.Clamp(p.Health, 0f, 1f));
    }

    public static string StageName(PlantStage s) => s switch
    {
        PlantStage.Germination => "всходы",
        PlantStage.Vegetative => "рост",
        PlantStage.Flowering => "цветение",
        PlantStage.Ripe => "созрело",
        _ => "погибло"
    };
}