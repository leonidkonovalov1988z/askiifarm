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
        var data = storage.LoadLatest<GameDataV0_0_3>(settings.MapPath);

        var world = new World();

        TilePalette palette;
        KeyBindings _; // не нужен здесь, но загружаем для единообразия
        CharacterPalette charPalette;

        if (settings.UseJsonContent && Directory.Exists(settings.ContentRoot))
        {
            (palette, _, charPalette) = GameContent.LoadFromFiles(settings.ContentRoot);
        }
        else
        {
            palette = GameContent.CreatePalette();
            charPalette = GameContent.CreateCharacterPalette();
        }

        world.SetResource(palette);
        world.SetResource(charPalette);
        world.SetResource(new GameState());
        world.SetResource(new CommandBuffer());
        world.SetResource(new FpsCounter());
        world.SetResource(new FpsLimiter(settings.SimulationFps));
        world.SetResource(settings);

        var map = world.CreateEntity();
        world.Add(map, new TileMapComponent { Tiles = data.Map });

        var player = world.CreateEntity();
        world.Add(player, new Position(data.PlayerX, data.PlayerY));
        world.Add(player, new PlayerTag());
        world.Add(player, charPalette.Get(settings.PlayerCharacterKey));

        return world;
    }
}