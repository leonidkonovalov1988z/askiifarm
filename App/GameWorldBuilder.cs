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

        // Создаём реестр сериализаторов (авто-регистрация из сборки)
        var registry = ComponentSerializerRegistry.AutoRegister(Assembly.GetExecutingAssembly());
        var saveSystem = new ComponentSaveSystem(registry);

        // Загружаем мир из файла сохранения
        if (ComponentSaveSystem.IsNewFormat(settings.MapPath))
        {
            saveSystem.Load(settings.MapPath, world);
        }
        else if (File.Exists(settings.MapPath))
        {
            // Legacy формат — загружаем и конвертируем
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

            // Сохраняем в новом формате
            saveSystem.Save(settings.MapPath, world);
        }
        else
        {
            // Файла нет — создаём мир по умолчанию
            // (здесь можно загрузить map_raw.txt или создать процедурно)
            throw new FileNotFoundException($"Файл сохранения не найден: {settings.MapPath}");
        }

        // Загружаем ресурсы (не сохраняются, всегда из JSON или хардкода)
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

        return world;
    }
}