// Systems/FarmSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Systems;

[GameSystem(Order = 7)]
public sealed class FarmSystem : ISystem
{
    public void Update(World world)
    {
        var actors = world.Query<FarmIntent, Position>();
        if (actors.Length == 0) return;

        var intents = world.Store<FarmIntent>();
        var positions = world.Store<Position>();
        var catalog = world.Resource<CropCatalog>();
        var selection = world.Resource<FarmSelection>();
        var log = world.Resource<FarmLog>();
        var state = world.Resource<GameState>();

        foreach (var actor in actors)
        {
            var action = intents.Get(actor).Action;
            var pos = positions.Get(actor);

            switch (action)
            {
                case FarmAction.CycleCrop:
                    if (catalog.Count > 0)
                    {
                        selection.Index = (selection.Index + 1) % catalog.Count;
                        log.Set($"Выбрано: {catalog.ByIndex(selection.Index)!.Name}", state.ElapsedTime);
                    }
                    break;

                case FarmAction.Plant: Plant(world, actor, pos); break;
                case FarmAction.Harvest: Harvest(world, actor, pos); break;
            }

            intents.Remove(actor);
        }
    }

    private static void Plant(World world, Entity actor, Position pos)
    {
        var catalog = world.Resource<CropCatalog>();
        var selection = world.Resource<FarmSelection>();
        var log = world.Resource<FarmLog>();
        var state = world.Resource<GameState>();

        var crop = catalog.ByIndex(selection.Index);
        if (crop == null) { log.Set("Нет культур (slavic_crops.csv)", state.ElapsedTime); return; }

        var maps = world.Query<TileMapComponent>();
        if (maps.Length == 0) return;
        var map = world.Store<TileMapComponent>().Get(maps[0]);
        if (pos.X < 0 || pos.Y < 0 || pos.X >= map.Width || pos.Y >= map.Height) return;

        var style = world.Resource<TilePalette>().Resolve(map.Tiles[pos.X, pos.Y].Symbol);
        if (style.BlocksMovement) { log.Set("Здесь нельзя сеять", state.ElapsedTime); return; }
        if (PlantAt(world, pos).HasValue) { log.Set("Тайл уже занят", state.ElapsedTime); return; }

        var granary = EnsureGranary(world, actor);

        // Двулетние: сначала семена (1-й год), при их отсутствии — корнеплод/кочан (2-й год).
        bool year2 = false;
        string key = crop.Id;
        if (crop.Mode == HarvestMode.Biennial
            && Granary.Get(granary, crop.Id) < crop.SeedRatio
            && Granary.Get(granary, Granary.Root(crop.Id)) >= 1f)
        {
            key = Granary.Root(crop.Id);
            year2 = true;
        }

        float cost = year2 ? 1f : crop.SeedRatio;
        if (Granary.Get(granary, key) < cost) { log.Set($"Нет семян: {crop.Name}", state.ElapsedTime); return; }
        Granary.Take(granary, key, cost);

        var plant = world.CreateEntity();
        world.Add(plant, new Position(pos.X, pos.Y));
        world.Add(plant, new PlantComponent
        {
            CropId = crop.Id,
            Health = 1f,
            Disease = 0f,
            GddAccumulated = 0,
            BiomassMj = 0,
            IsYear2 = year2,
            Stage = PlantStage.Germination
        });
        world.Add(plant, CropVisuals.For(crop, PlantStage.Germination));

        log.Set($"Посеяно: {crop.Name}{(year2 ? " (2 год)" : "")}", state.ElapsedTime);
    }

    private static void Harvest(World world, Entity actor, Position pos)
    {
        var log = world.Resource<FarmLog>();
        var state = world.Resource<GameState>();
        var balance = world.Resource<CropBalance>();
        var catalog = world.Resource<CropCatalog>();

        var found = PlantAt(world, pos);
        if (!found.HasValue) { log.Set("Здесь ничего не растёт", state.ElapsedTime); return; }
        var entity = found.Value;

        var plants = world.Store<PlantComponent>();
        var p = plants.Get(entity);
        if (!catalog.TryGet(p.CropId, out var crop)) { RemovePlant(world, entity); return; }

        void NotReady()
        {
            double target = crop.TargetGdd(p.IsYear2);
            int pct = target > 0 ? (int)Math.Clamp(p.GddAccumulated / target * 100.0, 0, 100) : 0;
            log.Set($"{crop.Name}: {CropMath.StageName(p.Stage)} {pct}%", state.ElapsedTime);
        }

        if (p.Stage == PlantStage.Dead || p.Health <= 0f)
        {
            RemovePlant(world, entity);
            ApplyFertility(world, pos, crop.FertilityDelta, balance);
            log.Set($"{crop.Name}: растение погибло", state.ElapsedTime);
            return;
        }

        var granary = EnsureGranary(world, actor);
        float yield = CropMath.Yield(p, crop, balance.MaxYieldMultiplier);
        string message;

        switch (crop.Mode)
        {
            case HarvestMode.GrainAndSeed:
                if (p.GddAccumulated < crop.GddHarvest) { NotReady(); return; }
                Granary.Add(granary, crop.Id, yield);
                message = $"{crop.Name}: {yield:F2} зерна";
                break;

            case HarvestMode.Biennial:
                if (!p.IsYear2)
                {
                    if (p.GddAccumulated < crop.GddHarvest) { NotReady(); return; }
                    Granary.Add(granary, Granary.Vegetables, yield);
                    Granary.Add(granary, Granary.Root(crop.Id), 1f);
                    message = $"{crop.Name}: {yield:F2} овощей";
                }
                else
                {
                    if (p.GddAccumulated < crop.TargetGdd(true)) { NotReady(); return; }
                    Granary.Add(granary, crop.Id, yield);
                    message = $"{crop.Name}: {yield:F2} семян";
                }
                break;

            default: // EarlyOrLate
                if (p.GddAccumulated >= crop.GddHarvest)
                {
                    Granary.Add(granary, Granary.Fiber, yield * 0.7f);
                    Granary.Add(granary, crop.Id, yield);
                    message = $"{crop.Name}: волокно+семена";
                }
                else if (crop.GddEarlyHarvest > 0 && p.GddAccumulated >= crop.GddEarlyHarvest)
                {
                    Granary.Add(granary, Granary.Fiber, yield);
                    message = $"{crop.Name}: {yield:F2} волокна";
                }
                else { NotReady(); return; }
                break;
        }

        RemovePlant(world, entity);
        ApplyFertility(world, pos, crop.FertilityDelta, balance);
        log.Set(message, state.ElapsedTime);
    }

    private static Entity? PlantAt(World world, Position pos)
    {
        var positions = world.Store<Position>();
        foreach (var e in world.Query<PlantComponent, Position>())
        {
            var p = positions.Get(e);
            if (p.X == pos.X && p.Y == pos.Y) return e;
        }
        return null;
    }

    private static void RemovePlant(World world, Entity e)
    {
        world.Store<PlantComponent>().Remove(e);
        world.Store<Glyph>().Remove(e);
        world.Store<Position>().Remove(e);
    }

    private static GranaryComponent EnsureGranary(World world, Entity actor)
    {
        var store = world.Store<GranaryComponent>();
        if (store.Has(actor))
        {
            var existing = store.Get(actor);
            if (existing.Items != null) return existing;
        }

        var granary = new GranaryComponent
        {
            Items = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        };
        float start = world.Resource<CropBalance>().StartingSeedUnits;
        foreach (var crop in world.Resource<CropCatalog>().All)
            granary.Items[crop.Id] = start;

        store.Set(actor, granary);
        return granary;
    }

    private static void ApplyFertility(World world, Position pos, float delta, CropBalance balance)
    {
        var maps = world.Query<TileMapComponent>();
        if (maps.Length == 0) return;

        var mapEntity = maps[0];
        var map = world.Store<TileMapComponent>().Get(mapEntity);
        var store = world.Store<SoilFertilityMap>();

        SoilFertilityMap field;
        if (store.Has(mapEntity) && store.Get(mapEntity).Values != null)
        {
            field = store.Get(mapEntity);
        }
        else
        {
            var values = new float[map.Width, map.Height];
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    values[x, y] = balance.MaxFertility;
            field = new SoilFertilityMap { Values = values };
        }

        if (pos.X < 0 || pos.Y < 0 || pos.X >= field.Width || pos.Y >= field.Height) return;
        field.Values[pos.X, pos.Y] = Math.Clamp(field.Values[pos.X, pos.Y] + delta,
                                                balance.MinFertility, balance.MaxFertility);
        store.Set(mapEntity, field);
    }
}