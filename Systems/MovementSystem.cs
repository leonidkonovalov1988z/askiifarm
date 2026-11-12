// Systems/MovementSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;
namespace AsciiFarm.Systems;

/// <summary>Бывший MapPhysics: проходимость берётся из данных палитры, а не из switch.</summary>
public sealed class MovementSystem : ISystem
{
    public void Update(World world)
    {
        var maps = world.Query<TileMapComponent>();
        if (maps.Length == 0) return;

        var map = world.Store<TileMapComponent>().Get(maps[0]);
        var palette = world.Resource<TilePalette>();
        var positions = world.Store<Position>();
        var intents = world.Store<MoveIntent>();

        foreach (var entity in world.Query<Position, MoveIntent>())
        {
            var intent = intents.Get(entity);
            var position = positions.Get(entity);
            int targetX = position.X + intent.Dx;
            int targetY = position.Y + intent.Dy;

            if (IsWalkable(map, palette, targetX, targetY))
                positions.Set(entity, new Position(targetX, targetY));

            intents.Remove(entity);   // намерение одноразовое
        }
    }

    private static bool IsWalkable(TileMapComponent map, TilePalette palette, int x, int y)
    {
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) return false;
        return !palette.Resolve(map.Tiles[x, y]).BlocksMovement;
    }
}