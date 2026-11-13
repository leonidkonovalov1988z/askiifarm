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
    // App/GameWorldBuilder.cs
    public static World Build(GameSettings settings)
    {
        var world = new World();

        var registry = ComponentSerializerRegistry.AutoRegister(Assembly.GetExecutingAssembly());
        var saveSystem = new ComponentSaveSystem(registry);

        // Загружаем компоненты из сохранения (если есть)
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

        // Ресурсы (не сохраняются, создаём всегда)
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
        world.SetResource(new CommandBuffer());
        world.SetResource(new FpsCounter());
        world.SetResource(new FpsLimiter(settings.SimulationFps));
        world.SetResource(settings);

        // Загрузка погоды
        var weatherData = WeatherCsvLoader.Load(settings.WeatherCsvPath, out var weatherError);
        if (settings.DaysPerMonth != 0)
        {
            weatherData = WeatherCsvLoader.CompressWeatherData(weatherData, settings.DaysPerMonth);
        }

        // Вычисляем StartIndex математически
        int startIndex;
        if (settings.DaysPerMonth == 0)
        {
            // Реальный календарь
            int dayOfYear = settings.GameStartDate.DayOfYear - 1; // 0-based
            startIndex = dayOfYear * 24 + settings.GameStartDate.Hour;
        }
        else
        {
            // Кастомный календарь
            int month = settings.GameStartDate.Month;
            int day = settings.GameStartDate.Day;
            if (month > 12) month = 12;
            if (day > settings.DaysPerMonth) day = settings.DaysPerMonth;
            int dayOfYear = (month - 1) * settings.DaysPerMonth + (day - 1);
            startIndex = dayOfYear * 24 + settings.GameStartDate.Hour;
        }

        // Ограничиваем в пределах данных
        startIndex = Math.Max(0, Math.Min(startIndex, weatherData.Count - 1));

        world.SetResource(new WeatherDataSource
        {
            HourlyData = weatherData,
            LoadError = weatherError,
            StartIndex = startIndex
        });
        world.SetResource(new WeatherCurrent());

        world.SetResource(new WeatherDataSource
        {
            HourlyData = weatherData,
            LoadError = weatherError,
            StartIndex = startIndex
        });
        world.SetResource(new WeatherCurrent());

        // СОЗДАЁМ GameState ВСЕГДА (независимо от сохранения)
        world.SetResource(new GameState
        {
            GameStartDate = settings.GameStartDate,
            TimeSpeed = settings.DefaultTimeSpeed,
            DefaultTimeSpeed = settings.DefaultTimeSpeed,
            MaxTimeStep = settings.MaxTimeStep,
            DaysPerMonth = settings.DaysPerMonth
        });

        return world;
    }
}