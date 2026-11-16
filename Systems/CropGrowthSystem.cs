// Systems/CropGrowthSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Systems;

[GameSystem(Order = 8)]
public sealed class CropGrowthSystem : ISystem
{
    private const int MaxHoursPerTick = 960;   // предохранитель; остаток догоняется на следующих тиках

    public void Update(World world)
    {
        var state = world.Resource<GameState>();
        var clock = world.Resource<CropClock>();
        var catalog = world.Resource<CropCatalog>();
        var balance = world.Resource<CropBalance>();

        long hour = (long)(state.GameTimeElapsed / 3600.0);
        if (clock.LastHour < 0) { clock.LastHour = hour; return; }

        int hours = (int)Math.Min(hour - clock.LastHour, MaxHoursPerTick);
        if (hours <= 0) return;
        if (catalog.Count == 0) { clock.LastHour = hour; return; }

        var entities = world.Query<PlantComponent, Position>();
        if (entities.Length == 0) { clock.LastHour = hour; return; }

        var plants = world.Store<PlantComponent>();
        var positions = world.Store<Position>();
        var glyphs = world.Store<Glyph>();
        var source = world.Resource<WeatherDataSource>();

        TileMapComponent? map = null;
        var maps = world.Query<TileMapComponent>();
        if (maps.Length > 0) map = world.Store<TileMapComponent>().Get(maps[0]);

        float[,]? fertility = null;
        var fertMaps = world.Query<SoilFertilityMap>();
        if (fertMaps.Length > 0) fertility = world.Store<SoilFertilityMap>().Get(fertMaps[0]).Values;

        double scale = balance.TimeScale(state.DaysPerMonth);

        for (int i = 0; i < hours; i++)
        {
            long absoluteHour = clock.LastHour + i;          // час, который растение проживает
            var weather = WeatherAt(source, absoluteHour);   // своя погода на каждый час (важно при промотке)

            foreach (var entity in entities)
            {
                var p = plants.Get(entity);
                if (p.Stage == PlantStage.Dead) continue;
                if (!catalog.TryGet(p.CropId, out var crop)) continue;

                var pos = positions.Get(entity);
                float soil = SoilMoisture(weather, map, pos, balance);
                float fert = FertilityAt(fertility, pos, balance);

                Step(ref p, crop, weather, soil, fert, scale, balance);

                var stage = CropMath.StageOf(p, crop);
                if (stage != p.Stage)
                {
                    p.Stage = stage;
                    if (glyphs.Has(entity)) glyphs.Set(entity, CropVisuals.For(crop, stage));
                }

                plants.Set(entity, p);
            }
        }

        clock.LastHour += hours;
    }

    private static void Step(ref PlantComponent p, CropDefinition c, in WeatherData w,
                             float soil, float fertility, double scale, CropBalance b)
    {
        var stage = CropMath.StageOf(p, c);
        float smMax = stage == PlantStage.Flowering ? c.RipeningMoistureMax : c.SoilMoistMax;

        // Шаг 1. Тепло (GDD-часы)
        if (w.AirTemp > c.TBase)
            p.GddAccumulated += (w.AirTemp - c.TBase) * scale;

        // Шаг 2. Стрессы
        float damage = 0f;
        if (w.GroundTemp < c.TCritColdGround)
            damage += (c.TCritColdGround - w.GroundTemp) * b.ColdDamageRate;
        if (stage == PlantStage.Flowering && w.AirTemp < c.TCritFrostFlower)
            damage += (c.TCritFrostFlower - w.AirTemp) * b.FrostDamageRate;
        if (w.AirTemp > c.TCritHeat)
            damage += (w.AirTemp - c.TCritHeat) * b.HeatDamageRate;
        if (soil < c.SoilMoistMin)
            damage += (c.SoilMoistMin - soil) * b.WaterDamageRate;
        if (soil > smMax)
            damage += (soil - smMax) * b.WaterDamageRate;

        if (w.AirHumidity > c.AirHumDiseaseMin && w.AirTemp > 5f)
            p.Disease += (w.AirHumidity - c.AirHumDiseaseMin) * b.DiseaseAccumRate * (float)scale;
        if (p.Disease > 1f)
            damage += (p.Disease - 1f) * b.DiseaseDamageRate;

        p.Health -= (float)(damage * scale);
        if (p.Health <= 0f)
        {
            p.Health = 0f;
            p.Stage = PlantStage.Dead;
            return;
        }

        // Шаг 3. Биомасса
        if (stage == PlantStage.Germination || w.SolarEnergyMj <= 0f) return;

        float tf = CropMath.Factor(w.AirTemp, c.TBase, c.TOptMin, c.TOptMax, c.TCritHeat);
        float wf = CropMath.Factor(soil, c.SoilMoistMin, c.SoilMoistOptMin, c.SoilMoistOptMax, smMax);
        if (tf <= 0f || wf <= 0f) return;

        p.BiomassMj += w.SolarEnergyMj * tf * wf * fertility * p.Health * scale;
    }

    private static WeatherData WeatherAt(WeatherDataSource source, long absoluteHour)
    {
        if (source.HourlyData.Count > 0)
        {
            int index = (int)((source.StartIndex + absoluteHour) % source.HourlyData.Count);
            return source.HourlyData[index];
        }

        // Нет CSV — мягкий дефолт, чтобы растения всё равно росли.
        int hourOfDay = (int)(((absoluteHour % 24) + 24) % 24);
        float sun = hourOfDay is >= 6 and <= 18
            ? MathF.Sin((hourOfDay - 6) / 12f * MathF.PI) * 1.2f
            : 0f;
        return new WeatherData
        {
            AirTemp = 16f,
            GroundTemp = 14f,
            AirHumidity = 0.6f,
            SoilMoisture = 0.55f,
            SolarEnergyMj = sun
        };
    }

    private static float SoilMoisture(in WeatherData w, TileMapComponent? map, Position pos, CropBalance b)
    {
        if (!b.UseTileMoisture || !map.HasValue) return w.SoilMoisture;
        var m = map.Value;
        if (pos.X < 0 || pos.Y < 0 || pos.X >= m.Width || pos.Y >= m.Height) return w.SoilMoisture;
        return Math.Clamp(m.Tiles[pos.X, pos.Y].Moisture, 0f, 1f);
    }

    private static float FertilityAt(float[,]? values, Position pos, CropBalance b)
    {
        if (values == null) return b.MaxFertility;
        if (pos.X < 0 || pos.Y < 0 || pos.X >= values.GetLength(0) || pos.Y >= values.GetLength(1))
            return b.MaxFertility;
        float v = values[pos.X, pos.Y];
        return v <= 0f ? b.MaxFertility : Math.Clamp(v, b.MinFertility, b.MaxFertility);
    }
}