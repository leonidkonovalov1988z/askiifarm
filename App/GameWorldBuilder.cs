// App/GameWorldBuilder.cs
using AsciiFarm.Components;
using AsciiFarm.Content;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Serialization;
using askiifarm.Serialization;
using System.Reflection;
namespace AsciiFarm.App;

public static class GameWorldBuilder
{
    public static World Build(GameSettings settings)
    {
        var world = new World();

        var registry = ComponentSerializerRegistry.AutoRegister(Assembly.GetExecutingAssembly());
        var saveSystem = new ComponentSaveSystem(registry);

        if (ComponentSaveSystem.IsNewFormat(settings.MapPath))
        {
            saveSystem.Load(settings.MapPath, world);
        }
        else if (File.Exists(settings.MapPath))
        {
            var legacyStorage = new GameStorage();
            var legacyData = legacyStorage.LoadLatest<GameDataV0_0_3>(settings.MapPath);

            var map = world.CreateEntity();
            world.Add(map, new TileMapComponent { Tiles = legacyData.Map });

            var player = world.CreateEntity();
            world.Add(player, new Position(legacyData.PlayerX, legacyData.PlayerY));
            world.Add(player, new PlayerTag());
            world.Add(player, new Glyph
            {
                Symbol = '☺',
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
                Layer = 10
            });

            saveSystem.Save(settings.MapPath, world);
        }
        else
        {
            throw new FileNotFoundException($"Файл сохранения не найден: {settings.MapPath}");
        }

        TilePalette palette;
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

        // Загрузка погоды
        var weatherData = WeatherCsvLoader.Load(settings.WeatherCsvPath, out var weatherError);
        int startIndex = WeatherCsvLoader.FindStartIndex(weatherData, settings.GameStartDate);

        world.SetResource(new WeatherDataSource
        {
            HourlyData = weatherData,
            LoadError = weatherError,
            StartIndex = startIndex  // НОВОЕ
        });
        world.SetResource(new WeatherCurrent());

        // Инициализация GameState с датой из настроек
        var gameState = world.Resource<GameState>();
        gameState.GameStartDate = settings.GameStartDate;  // НОВОЕ
        gameState.TimeSpeed = 1.0f;
        gameState.MaxTimeStep = settings.MaxTimeStep;

        return world;
    }
}