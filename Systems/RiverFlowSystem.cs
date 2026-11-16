// Systems/RiverFlowSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Systems;

[GameSystem(Order = 5)]
public sealed class RiverFlowSystem : ISystem
{
    private const char WaterSymbol = '≈';
    private const float AnimationSpeed = 1f;
    private const float BlueProbability = 0.5f;

    public void Update(World world)
    {
        var cache = world.Resource<WaterColorCache>();
        var state = world.Resource<GameState>();

        int bucket = (int)(state.ElapsedTime * AnimationSpeed);
        if (bucket == cache.LastBucket) return;   // картинка не изменилась — не считаем
        cache.LastBucket = bucket;
        cache.Colors.Clear();                     // словарь переиспользуется

        var maps = world.Query<TileMapComponent>();
        if (maps.Length == 0) return;
        var map = world.Store<TileMapComponent>().Get(maps[0]);

        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (map.Tiles[x, y].Symbol != WaterSymbol) continue;
                int hash = HashCode.Combine(x, y, bucket);
                float value = (hash & 0xFFFF) / (float)0xFFFF;
                var fg = value < BlueProbability ? ConsoleColor.Cyan : ConsoleColor.White;
                cache.Colors[(x, y)] = new TileStyle(fg, ConsoleColor.Black, false);
            }
    }
}