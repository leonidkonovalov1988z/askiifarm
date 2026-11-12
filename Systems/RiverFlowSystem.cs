// Systems/RiverFlowSystem.cs
using System.Collections.Generic;
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;
namespace AsciiFarm.Systems;

/// <summary>
/// Анимация реки: каждый водный тайл с шансом 80% синий, 20% белый.
/// Псевдослучайность детерминирована по (x, y, time).
/// </summary>
[GameSystem(Order = 5)]
public sealed class RiverFlowSystem : ISystem
{
    private const char WaterSymbol = '≈';
    private const float AnimationSpeed = 1f;
    private const float BlueProbability = 0.5f;

    public void Update(World world)
    {
        var state = world.Resource<GameState>();
        var maps = world.Query<TileMapComponent>();
        if (maps.Length == 0) return;

        var map = world.Store<TileMapComponent>().Get(maps[0]);
        float time = (float)state.ElapsedTime;

        var waterColors = new Dictionary<(int, int), TileStyle>();

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                if (map.Tiles[x, y] != WaterSymbol) continue;

                // Детерминированный псевдослучайный выбор цвета
                int hash = HashCode.Combine(x, y, (int)(time * AnimationSpeed));
                float randomValue = (hash & 0xFFFF) / (float)0xFFFF;

                ConsoleColor fg = randomValue < BlueProbability
                    ? ConsoleColor.Cyan
                    : ConsoleColor.White;

                waterColors[(x, y)] = new TileStyle(fg, ConsoleColor.Black, false);
            }
        }

        world.SetResource(new WaterColorCache(waterColors));
    }
}

/// <summary>Кэш цветов воды на текущий кадр. Данные.</summary>
public sealed class WaterColorCache
{
    public Dictionary<(int, int), TileStyle> Colors { get; }
    public WaterColorCache(Dictionary<(int, int), TileStyle> colors) => Colors = colors;
}