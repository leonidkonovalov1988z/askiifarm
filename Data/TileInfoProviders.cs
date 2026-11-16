// Data/TileInfoProviders.cs
using System.Collections.Generic;
using AsciiFarm.Components;
using AsciiFarm.Core;

namespace AsciiFarm.Data;

/// <summary>
/// Выводит базовые свойства местности (NPK, влажность, проходимость) из TileData.
/// </summary>
public sealed class TerrainInfoProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(int x, int y, char tile, World world)
    {
        var maps = world.Query<TileMapComponent>();
        if (maps.Length == 0) yield break;

        var map = world.Store<TileMapComponent>().Get(maps[0]);
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) yield break;

        var data = map.Tiles[x, y];

        yield return $"Символ: '{tile}'";
        yield return $"N: {data.Nitrogen:P0} | P: {data.Phosphorus:P0} | K: {data.Potassium:P0}";
        yield return $"Влага почвы: {data.Moisture:P0}";

        var palette = world.Resource<TilePalette>();
        var style = palette.Resolve(tile);
        yield return $"Проходимость: {(style.BlocksMovement ? "Нет" : "Да")}";
    }
}

/// <summary>
/// АВТОМАТИЧЕСКИЙ ИНСПЕКТОР: находит ВСЕ сущности на данных координатах 
/// и выводит их компоненты через ComponentDisplayRegistry.
/// </summary>
// Data/TileInfoProviders.cs — заменить EntityInspectorProvider (остальные провайдеры без изменений)
public sealed class EntityInspectorProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(int x, int y, char tile, World world)
    {
        var positions = world.Store<Position>();

        foreach (var entity in positions.Entities)
        {
            var pos = positions.Get(entity);
            if (pos.X != x || pos.Y != y) continue;

            yield return $"── Объект (ID: {entity.Id}) ──";

            var visitor = new InspectorVisitor(entity);
            foreach (var (_, store) in world.AllStores()) store.Accept(visitor);
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
            if (typeof(T) == typeof(Position)) return;   // координаты уже показаны
            Lines.Add($"  {typeof(T).Name}: {ComponentDisplayRegistry.Format(in component)}");
        }
    }
}
/// <summary>Информация о погоде (глобальная, не зависит от X,Y, но оставим для совместимости).</summary>
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

/// <summary>Информация о растении на тайле + плодородие почвы.</summary>
public sealed class PlantInfoProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(int x, int y, char tile, World world)
    {
        var catalog = world.Resource<CropCatalog>();
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
            yield return $"Здоровье: {p.Health:P0}";
            yield return $"Болезнь: {p.Disease:F2}";
            yield return $"Биомасса: {p.BiomassMj:F0}/{c.SolarMjTarget:F0}";
            yield return $"Прогноз: {CropMath.Yield(p, c):F2} ед.";
        }

        var fields = world.Query<SoilFertilityMap>();
        if (fields.Length > 0)
        {
            var field = world.Store<SoilFertilityMap>().Get(fields[0]);
            if (x >= 0 && y >= 0 && x < field.Width && y < field.Height)
                yield return $"Плодородие: {field.Values[x, y]:P0}";
        }
    }
}