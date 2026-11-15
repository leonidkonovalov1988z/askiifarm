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
public sealed class EntityInspectorProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(int x, int y, char tile, World world)
    {
        var positions = world.Store<Position>();

        // Ищем все сущности, чья позиция совпадает с (x, y)
        foreach (var entity in positions.Entities)
        {
            var pos = positions.Get(entity);
            if (pos.X == x && pos.Y == y)
            {
                yield return $"── Объект (ID: {entity.Id}) ──";

                // Перебираем все хранилища компонентов в мире
                foreach (var (compType, store) in world.AllStores())
                {
                    // Проверяем, есть ли у этой сущности данный компонент
                    if (store.TryGetBoxed(entity, out var comp))
                    {
                        // Координаты мы уже показали, не дублируем
                        if (compType == typeof(Position)) continue;

                        // Используем реестр для красивого вывода
                        string display = ComponentDisplayRegistry.Format(comp);
                        yield return $"  {compType.Name}: {display}";
                    }
                }
            }
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