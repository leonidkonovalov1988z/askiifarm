// Systems/CropGrowthSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Systems;

[GameSystem(Order = 8)]
public sealed class CropGrowthSystem : ISystem
{
    private const int MaxHoursPerTick = 960;

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

        double scale = balance.TimeScale(state.DaysPerMonth);

        for (int i = 0; i < hours; i++)
        {
            long absoluteHour = clock.LastHour + i;
            var weather = WeatherAt(source, absoluteHour);

            foreach (var entity in entities)
            {
                var p = plants.Get(entity);
                if (p.Stage == PlantStage.Dead) continue;
                if (!catalog.TryGet(p.CropId, out var crop)) continue;

                var pos = positions.Get(entity);
                float soil = SoilMoisture(weather, map, pos, balance);
                float nutrient = NutrientFactor(map, pos, balance);

                double before = p.BiomassMj;
                Step(ref p, crop, weather, soil, nutrient, scale, balance);
                double gained = p.BiomassMj - before;

                // Вынос N/P/K пропорционально приросту биомассы — каждый час.
                if (gained > 0) ApplyUptake(map, pos, crop, gained);

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
                             float soil, float nutrient, double scale, CropBalance b)
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

        p.BiomassMj += w.SolarEnergyMj * tf * wf * nutrient * p.Health * scale;
    }

    /// <summary>Списание (или обогащение) N/P/K прямо в TileData. Массив общий — изменения видны всем.</summary>
    private static void ApplyUptake(TileMapComponent? map, Position pos, CropDefinition c, double gainedMj)
    {
        if (!map.HasValue || c.SolarMjTarget <= 0 || c.SoilDelta == 0f) return;
        var m = map.Value;
        if (pos.X < 0 || pos.Y < 0 || pos.X >= m.Width || pos.Y >= m.Height) return;

        float share = (float)(gainedMj / c.SolarMjTarget);
        float delta = c.SoilDelta * share;

        ref var tile = ref m.Tiles[pos.X, pos.Y];
        tile.Nitrogen = Math.Clamp(tile.Nitrogen + delta, 0f, 1f);
        tile.Phosphorus = Math.Clamp(tile.Phosphorus + delta, 0f, 1f);
        tile.Potassium = Math.Clamp(tile.Potassium + delta, 0f, 1f);
    }

    private static float NutrientFactor(TileMapComponent? map, Position pos, CropBalance b)
    {
        if (!map.HasValue) return 1f;
        var m = map.Value;
        if (pos.X < 0 || pos.Y < 0 || pos.X >= m.Width || pos.Y >= m.Height) return 1f;
        return CropMath.NutritionOf(m.Tiles[pos.X, pos.Y], b).Factor;
    }

    private static WeatherData WeatherAt(WeatherDataSource source, long absoluteHour)
    {
        if (source.HourlyData.Count > 0)
        {
            int index = (int)((source.StartIndex + absoluteHour) % source.HourlyData.Count);
            return source.HourlyData[index];
        }

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
}