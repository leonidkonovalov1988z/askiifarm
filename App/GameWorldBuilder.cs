// App/GameWorldBuilder.cs
using AsciiFarm.Components;
using AsciiFarm.Content;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Rendering;
using AsciiFarm.Serialization;
using AsciiFarm.Systems;
using System.Reflection;

namespace AsciiFarm.App;

public static class GameWorldBuilder
{
    public static World Build(GameSettings settings)
    {
        var world = new World();

        // P2-17: все пути проходят песочницу рабочей директории
        string mapPath = SafePath.Resolve(settings.MapPath);
        string weatherPath = SafePath.Resolve(settings.WeatherCsvPath);

        var registry = ComponentSerializerRegistry.AutoRegister(Assembly.GetExecutingAssembly());
        var saveSystem = new ComponentSaveSystem(registry);

        if (ComponentSaveSystem.IsNewFormat(mapPath))
        {
            saveSystem.Load(mapPath, world);
        }
        else if (File.Exists(mapPath))
        {
            // МИГРАЦИЯ: старый формат -> новый
            var legacyData = new GameStorage().LoadLatest<GameDataV0_0_3>(mapPath);

            var map = world.CreateEntity();
            world.Add(map, new TileMapComponent { Tiles = MigrateOldMap(legacyData.Map) });

            var player = world.CreateEntity();
            world.Add(player, new Position(legacyData.PlayerX, legacyData.PlayerY));
            world.Add(player, new PlayerTag());
            world.Add(player, new Glyph
            {
                Symbol = Cp437Symbols.WhiteSmilingFace,
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
                Layer = 10
            });

            saveSystem.Save(mapPath, world);
        }
        else
        {
            throw new FileNotFoundException($"Файл сохранения не найден: {mapPath}");
        }



        // --- Контент ---
        TilePalette palette;
        CharacterPalette charPalette;
        if (settings.UseJsonContent && Directory.Exists(settings.ContentRoot))
            (palette, _, charPalette) = GameContent.LoadFromFiles(settings.ContentRoot);
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

        // --- Растения ---
        var cropCatalog = CropCsvLoader.Load(SafePath.Resolve(settings.CropsCsvPath));
        world.SetResource(cropCatalog);
        world.SetResource(new CropBalance { StartingSeedUnits = settings.StartingSeedUnits });
        world.SetResource(new CropClock());
        world.SetResource(new FarmSelection());
        world.SetResource(new FarmLog());

        // Компактный вывод компонентов в инспекторе тайла
        ComponentDisplayRegistry.Register<Glyph>(g => $"'{g.Symbol}' слой {g.Layer}");
        ComponentDisplayRegistry.Register<PlantComponent>(p => $"{p.CropId}, {p.Stage}, HP {p.Health:P0}");
        ComponentDisplayRegistry.Register<GranaryComponent>(g => $"{g.Items?.Count ?? 0} позиций");

        // --- P2-10: состояние систем живёт в ресурсах ---
        world.SetResource(new SimulationClock());
        world.SetResource(new WeatherSystemState());
        world.SetResource(new WaterColorCache());

        // --- Погода (P0-1: ровно одна регистрация каждого ресурса) ---
        var weatherData = WeatherCsvLoader.Load(weatherPath, out var weatherError);
        if (settings.DaysPerMonth != 0)
            weatherData = WeatherCsvLoader.CompressWeatherData(weatherData, settings.DaysPerMonth);

        int startIndex = ComputeStartIndex(settings, weatherData.Count);
        world.SetResource(new WeatherDataSource
        {
            HourlyData = weatherData,
            LoadError = weatherError,
            StartIndex = startIndex
        });
        world.SetResource(new WeatherCurrent());

        world.SetResource(new GameState
        {
            GameStartDate = settings.GameStartDate,
            TimeSpeed = settings.DefaultTimeSpeed,
            DefaultTimeSpeed = settings.DefaultTimeSpeed,
            MaxTimeStep = settings.MaxTimeStep,
            DaysPerMonth = settings.DaysPerMonth
        });

        world.SetResource(new HudMetrics());   // геометрия HUD: OriginX/ColumnWidth можно крутить

        // --- Провайдеры информации о тайле (P0-1: каждый ровно один раз) ---
        var tileInfoRegistry = new TileInfoRegistry();
        tileInfoRegistry.Register(new TerrainInfoProvider());
        tileInfoRegistry.Register(new PlantInfoProvider());
        tileInfoRegistry.Register(new EntityInspectorProvider());
        // WeatherTileInfoProvider убран: дублирует панель ПОГОДА
        world.SetResource(tileInfoRegistry);

        return world;   // P0-1: единственный return
    }

    private static int ComputeStartIndex(GameSettings settings, int dataCount)
    {
        if (dataCount == 0) return 0;

        int startIndex;
        if (settings.DaysPerMonth == 0)
        {
            startIndex = (settings.GameStartDate.DayOfYear - 1) * 24 + settings.GameStartDate.Hour;
        }
        else
        {
            int month = Math.Clamp(settings.GameStartDate.Month, 1, 12);
            int day = Math.Clamp(settings.GameStartDate.Day, 1, settings.DaysPerMonth);
            int dayOfYear = (month - 1) * settings.DaysPerMonth + (day - 1);
            startIndex = dayOfYear * 24 + settings.GameStartDate.Hour;
        }
        return Math.Clamp(startIndex, 0, dataCount - 1);
    }

    private static TileData[,] MigrateOldMap(char[,] oldMap)
    {
        int width = oldMap.GetLength(0), height = oldMap.GetLength(1);
        var newMap = new TileData[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                char c = oldMap[x, y];
                newMap[x, y] = c switch
                {
                    '≈' => new TileData { Symbol = c, Moisture = 1.0f },
                    '♣' or '♠' => new TileData { Symbol = c, Nitrogen = 0.7f, Phosphorus = 0.5f, Potassium = 0.6f, Moisture = 0.7f },
                    '.' or ',' or '\'' or '`' or ';' => new TileData { Symbol = c, Nitrogen = 0.5f, Phosphorus = 0.3f, Potassium = 0.4f, Moisture = 0.6f },
                    '#' or '▒' or '┌' or '┐' or '└' or '┘' or '─' or '│' or '┬' or '┴' or '├' or '┤' or '┼' or '╪'
                        or '╔' or '╗' or '╚' or '╝' or '═' or '║' or '╠' or '╣' or '╦' or '╩' or '╬'
                        => new TileData { Symbol = c },
                    ' ' => new TileData { Symbol = c, Nitrogen = 0.1f, Phosphorus = 0.1f, Potassium = 0.1f, Moisture = 0.1f },
                    _ => new TileData { Symbol = c, Nitrogen = 0.3f, Phosphorus = 0.3f, Potassium = 0.3f, Moisture = 0.4f }
                };
            }
        return newMap;
    }
}