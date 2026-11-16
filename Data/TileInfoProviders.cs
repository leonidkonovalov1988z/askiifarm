// Data/TileInfoProviders.cs
using System.Collections.Generic;
using AsciiFarm.Components;
using AsciiFarm.Core;

namespace AsciiFarm.Data;

/// <summary>Свойства местности: сырые NPK, влага, проходимость.</summary>
public sealed class TerrainInfoProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(int x, int y, char tile, World world)
    {
        var maps = world.Query<TileMapComponent>();
        if (maps.Length == 0) yield break;
        var map = world.Store<TileMapComponent>().Get(maps[0]);
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) yield break;

        var d = map.Tiles[x, y];
        yield return $"Символ: '{tile}'";
        yield return $"N:{d.Nitrogen * 100:F0}% P:{d.Phosphorus * 100:F0}% K:{d.Potassium * 100:F0}%";
        yield return $"Влага: {d.Moisture * 100:F0}%";
        var style = world.Resource<TilePalette>().Resolve(tile);
        yield return $"Проходимость: {(style.BlocksMovement ? "Нет" : "Да")}";
    }
}

/// <summary>Инспектор компонентов. Скрыто то, что уже показано отдельными блоками.</summary>
public sealed class EntityInspectorProvider : ITileInfoProvider
{
    private static readonly HashSet<Type> Hidden = new()
    {
        typeof(Position), typeof(Glyph), typeof(PlantComponent),
        typeof(GranaryComponent), typeof(TileMapComponent),
        typeof(MoveIntent), typeof(FarmIntent)
    };

    public IEnumerable<string> GetInfo(int x, int y, char tile, World world)
    {
        var positions = world.Store<Position>();
        foreach (var entity in positions.Entities)
        {
            var pos = positions.Get(entity);
            if (pos.X != x || pos.Y != y) continue;

            var visitor = new InspectorVisitor(entity);
            foreach (var (_, store) in world.AllStores()) store.Accept(visitor);
            if (visitor.Lines.Count == 0) continue;

            yield return $"── Объект (ID: {entity.Id}) ──";
            foreach (var line in visitor.Lines) yield return line;
        }
    }

    private sealed class InspectorVisitor : IComponentVisitor
    {
        private readonly int _targetId;
        public List<string> Lines { get; } = new();
        public InspectorVisitor(Entity target) => _targetId = target.Id;

        public void Visit<T>(Entity entity, in T component) where T : struct
        {
            if (entity.Id != _targetId) return;
            if (Hidden.Contains(typeof(T))) return;
            Lines.Add($"  {typeof(T).Name}: {ComponentDisplayRegistry.Format(in component)}");
        }
    }
}

/// <summary>Совместимость; в HUD не регистрируется (дублирует панель ПОГОДА).</summary>
public sealed class WeatherTileInfoProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(int x, int y, char tile, World world)
    {
        var weather = world.Resource<WeatherCurrent>();
        if (!weather.HasData) yield break;
        var d = weather.Current;
        yield return "── Погода ──";
        yield return $"Воздух: {d.AirTemp:F1}°C";
        yield return $"Влажн: {d.AirHumidity * 100:F0}%";
        yield return $"Снег: {d.EffectiveSnowCm:F1}см";
    }
}

/// <summary>Растение на тайле + питание, выведенное из NPK.</summary>
public sealed class PlantInfoProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(int x, int y, char tile, World world)
    {
        var catalog = world.Resource<CropCatalog>();
        var balance = world.Resource<CropBalance>();
        var plants = world.Store<PlantComponent>();
        var positions = world.Store<Position>();

        foreach (var entity in world.Query<PlantComponent, Position>())
        {
            var pos = positions.Get(entity);
            if (pos.X != x || pos.Y != y) continue;

            var p = plants.Get(entity);
            if (!catalog.TryGet(p.CropId, out var c)) continue;

            double target = c.TargetGdd(p.IsYear2);
            int pct = target > 0 ? (int)Math.Clamp(p.GddAccumulated / target * 100.0, 0, 100) : 0;

            yield return "── РАСТЕНИЕ ──";
            yield return $"{c.Name}{(p.IsYear2 ? " (2 год)" : "")}";
            yield return $"Стадия: {CropMath.StageName(p.Stage)}";
            yield return $"Созревание: {pct}%";
            yield return $"Здоровье: {p.Health * 100:F0}%";
            yield return $"Болезнь: {p.Disease:F2}";
            yield return $"Биомасса: {p.BiomassMj:F0}/{c.SolarMjTarget:F0}";
            yield return $"Прогноз: {CropMath.Yield(p, c):F2} ед.";

            var maps = world.Query<TileMapComponent>();
            if (maps.Length > 0)
            {
                var map = world.Store<TileMapComponent>().Get(maps[0]);
                if (x >= 0 && y >= 0 && x < map.Width && y < map.Height)
                {
                    var n = CropMath.NutritionOf(map.Tiles[x, y], balance);
                    string limit = n.Limiting.Length == 0 ? "" : $" ({n.Limiting})";
                    yield return $"Питание: {n.Factor * 100:F0}%{limit}";
                }
            }
        }
    }
}