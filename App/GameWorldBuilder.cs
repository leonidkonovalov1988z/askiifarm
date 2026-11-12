// App/GameWorldBuilder.cs
using AsciiFarm.Components;
using AsciiFarm.Content;
using AsciiFarm.Core;
using AsciiFarm.Data;
using askiifarm.Serialization;
namespace AsciiFarm.App;

public static class GameWorldBuilder
{
    public static World Build(GameSettings settings)
    {
        var storage = new GameStorage();
        var data = storage.LoadAndConvert<GameDataV0_0_1>(settings.MapPath, new Version(0, 0, 1));

        var world = new World();
        world.SetResource(GameContent.CreatePalette());
        world.SetResource(new GameState());
        world.SetResource(new CommandBuffer());

        var map = world.CreateEntity();
        world.Add(map, new TileMapComponent { Tiles = data.Map });

        var player = world.CreateEntity();
        world.Add(player, new Position(settings.PlayerStartX, settings.PlayerStartY));
        world.Add(player, GameContent.PlayerGlyph);
        world.Add(player, new PlayerTag());

        return world;
    }
}