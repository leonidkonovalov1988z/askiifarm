// Systems/SnapshotSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Rendering;
namespace AsciiFarm.Systems;

public sealed class SnapshotSystem
{
    private readonly FrameExchange<RenderFrame> _frames;

    public SnapshotSystem(FrameExchange<RenderFrame> frames) => _frames = frames;

    public void Update(World world)
    {
        var frame = _frames.WriteBuffer;
        var palette = world.Resource<TilePalette>();
        var gameState = world.Resource<GameState>();
        var characterPalette = world.Resource<CharacterPalette>();
        var fpsCounter = world.Resource<FpsCounter>();

        WaterColorCache? waterCache = null;
        try { waterCache = world.Resource<WaterColorCache>(); } catch { }

        Array.Fill(frame.Symbols, ' ');
        Array.Fill(frame.Foreground, palette.Fallback.Foreground);
        Array.Fill(frame.Background, palette.Fallback.Background);

        // Рисуем тайлы карты
        foreach (var entity in world.Query<TileMapComponent>())
            DrawTiles(frame, world.Store<TileMapComponent>().Get(entity), palette, waterCache);

        // Рисуем персонажей
        var glyphs = world.Store<Glyph>();
        var positions = world.Store<Position>();
        var playerTags = world.Store<PlayerTag>();

        foreach (var entity in world.Query<Glyph, Position>().OrderBy(e => glyphs.Get(e).Layer))
        {
            var glyph = glyphs.Get(entity);
            var pos = positions.Get(entity);
            if (playerTags.Has(entity))
                glyph = characterPalette.Get(characterPalette.DefaultPlayerKey);
            Put(frame, pos.X, pos.Y, glyph.Symbol, glyph.Foreground, glyph.Background);
        }

        // Рисуем UI в две колонки
        DrawTwoColumnUI(frame, world, gameState, fpsCounter);

        frame.Tick = gameState.Tick;
        _frames.Publish();
    }

    private static void DrawTiles(RenderFrame frame, TileMapComponent map,
        TilePalette palette, WaterColorCache? waterCache)
    {
        int width = Math.Min(map.Width, frame.Width);
        int height = Math.Min(map.Height, frame.Height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                char tile = map.Tiles[x, y].Symbol;
                var style = waterCache != null && waterCache.Colors.TryGetValue((x, y), out var ws)
                    ? ws
                    : palette.Resolve(tile);
                Put(frame, x, y, tile, style.Foreground, style.Background);
            }
    }

    // Systems/SnapshotSystem.cs — метод DrawTwoColumnUI
    /// <summary>Рисует UI в две колонки справа.</summary>
    private static void DrawTwoColumnUI(RenderFrame frame, World world,
        GameState state, FpsCounter fpsCounter)
    {
        int colWidth = 20;
        int gap = 2;
        int totalWidth = colWidth * 2 + gap;

        // Если экран слишком узкий, не рисуем UI
        if (frame.Width < totalWidth + 5) return;

        int startX = frame.Width - totalWidth;
        int col1X = startX;
        int col2X = startX + colWidth + gap;

        // Колонка 1: Дата/время (верх), информация о тайле (низ)
        DrawDateTimeColumn(frame, col1X, 0, state);
        DrawTileInfoColumn(frame, world, col1X, frame.Height - 12);

        // Колонка 2: FPS/TPS/Погода
        DrawStatsColumn(frame, col2X, 0, fpsCounter);
        DrawWeatherColumn(frame, col2X, 3, world);
    }

    /// <summary>Колонка 1 (верх): Дата и время.</summary>
    private static void DrawDateTimeColumn(RenderFrame frame, int startX, int startY, GameState state)
    {
        string gameTimeStr = CalendarHelper.FormatGameTime(
            state.GameStartDate, state.GameTimeElapsed, state.DaysPerMonth);
        string speedStr = $"x{state.TimeSpeed:F0}";
        TimeSpan realTime = TimeSpan.FromSeconds(state.ElapsedTime);
        string realTimeStr = $"{(int)realTime.TotalHours:D2}:{realTime.Minutes:D2}:{realTime.Seconds:D2}";

        string[] timeParts = gameTimeStr.Split(' ');
        string dateStr = timeParts.Length > 0 ? timeParts[0] : "";
        string timeOfDayStr = timeParts.Length > 1 ? timeParts[1] : "";

        int y = startY;
        DrawText(frame, startX, y++, "── ВРЕМЯ ──", ConsoleColor.Cyan);
        DrawText(frame, startX, y++, dateStr, ConsoleColor.White);
        DrawText(frame, startX, y++, timeOfDayStr, ConsoleColor.Yellow);
        DrawText(frame, startX, y++, $"Скорость: {speedStr}", ConsoleColor.Green);
        DrawText(frame, startX, y++, $"Реальное: {realTimeStr}", ConsoleColor.DarkGray);
    }

    /// <summary>Колонка 1 (низ): Информация о тайле под игроком.</summary>
    // Systems/SnapshotSystem.cs (найдите метод DrawTileInfoColumn)

    /// <summary>Колонка 1 (низ): Информация о тайле под игроком.</summary>
    private static void DrawTileInfoColumn(RenderFrame frame, World world, int startX, int startY)
    {
        var players = world.Query<PlayerTag, Position>();
        if (players.Length == 0) return;

        var positions = world.Store<Position>();
        var maps = world.Query<TileMapComponent>();
        if (maps.Length == 0) return;

        var map = world.Store<TileMapComponent>().Get(maps[0]);
        var playerPos = positions.Get(players[0]);

        if (playerPos.X < 0 || playerPos.Y < 0 ||
            playerPos.X >= map.Width || playerPos.Y >= map.Height)
            return;

        char tile = map.Tiles[playerPos.X, playerPos.Y].Symbol; // .Symbol добавлен из-за TileData

        // Получаем информацию, передавая координаты!
        var registry = world.Resource<TileInfoRegistry>();
        var infoLines = registry.GetAllInfo(playerPos.X, playerPos.Y, tile, world).ToList();

        int y = startY;
        DrawText(frame, startX, y++, "── ТАЙЛ ──", ConsoleColor.Cyan);
        DrawText(frame, startX, y++, $"Позиция: ({playerPos.X}, {playerPos.Y})", ConsoleColor.White);

        foreach (var line in infoLines)
        {
            if (y >= frame.Height) break;
            DrawText(frame, startX, y++, line, ConsoleColor.Gray);
        }
    }
    /// <summary>Колонка 2 (верх): FPS и TPS.</summary>
    private static void DrawStatsColumn(RenderFrame frame, int startX, int startY, FpsCounter fpsCounter)
    {
        int y = startY;
        DrawText(frame, startX, y++, "── СТАТИСТИКА ──", ConsoleColor.Cyan);
        DrawText(frame, startX, y++, $"FPS: {fpsCounter.RealFps}", ConsoleColor.Green);
        DrawText(frame, startX, y++, $"TPS: {fpsCounter.TicksPerSecond}", ConsoleColor.Cyan);
    }

    /// <summary>Колонка 2: Погода.</summary>
    private static void DrawWeatherColumn(RenderFrame frame, int startX, int startY, World world)
    {
        var weather = world.Resource<WeatherCurrent>();
        var source = world.Resource<WeatherDataSource>();

        int y = startY;

        if (!weather.HasData)
        {
            DrawText(frame, startX, y++, "── ПОГОДА ──", ConsoleColor.DarkGray);
            DrawText(frame, startX, y++, "нет данных", ConsoleColor.DarkGray);
            if (source.LoadError != null)
                DrawText(frame, startX, y++, source.LoadError, ConsoleColor.Red);
            return;
        }

        var d = weather.Current;
        DrawText(frame, startX, y++, "── ПОГОДА ──", ConsoleColor.Cyan);
        DrawText(frame, startX, y++, $"Час: {weather.HourIndex:D2}", ConsoleColor.DarkGray);
        DrawText(frame, startX, y++, $"Воздух: {d.AirTemp,7:F1}°C", ConsoleColor.White);
        DrawText(frame, startX, y++, $"Земля: {d.GroundTemp,7:F1}°C",
            d.GroundTemp < 0 ? ConsoleColor.Blue : ConsoleColor.White);
        DrawText(frame, startX, y++, $"Влажн: {d.AirHumidity * 100,6:F0}%", ConsoleColor.White);
        DrawText(frame, startX, y++, $"Почва: {d.SoilMoisture * 100,6:F0}%",
            d.SoilMoisture < 0.2f ? ConsoleColor.Red : ConsoleColor.Green);
        DrawText(frame, startX, y++, $"Солнц: {d.SolarEnergyMj,6:F2}МДж", ConsoleColor.Yellow);
        DrawText(frame, startX, y++, $"Снег: {d.EffectiveSnowCm,6:F1}см", ConsoleColor.White);
        DrawText(frame, startX, y++, $"Осад: {d.PrecipitationMm,6:F1}мм", ConsoleColor.Blue);
    }

    /// <summary>Вспомогательный метод для вывода текста.</summary>
    private static void DrawText(RenderFrame frame, int x, int y, string text, ConsoleColor color)
    {
        for (int i = 0; i < text.Length && x + i < frame.Width; i++)
        {
            if (y < frame.Height)
                Put(frame, x + i, y, text[i], color, ConsoleColor.Black);
        }
    }

    // В методе DrawUI:
    private static void DrawUI(RenderFrame frame, GameState state, FpsCounter fpsCounter)
    {
        string fpsStr = $"FPS: {fpsCounter.RealFps}";
        string tickStr = $"TPS: {fpsCounter.TicksPerSecond}";

        int startX = frame.Width - Math.Max(fpsStr.Length, tickStr.Length) - 1;

        for (int i = 0; i < fpsStr.Length; i++)
            if (startX + i < frame.Width)
                Put(frame, startX + i, 0, fpsStr[i], ConsoleColor.Green, ConsoleColor.Black);

        for (int i = 0; i < tickStr.Length; i++)
            if (startX + i < frame.Width && 1 < frame.Height)
                Put(frame, startX + i, 1, tickStr[i], ConsoleColor.Cyan, ConsoleColor.Black);
    }
    // Новый метод:
    private static void DrawWeatherUI(RenderFrame frame, WeatherCurrent weather, WeatherDataSource source)
    {
        int panelWidth = 24;
        int startX = frame.Width - panelWidth - 2;
        int startY = 3;

        (string text, ConsoleColor color)[] lines;

        if (!weather.HasData)
        {
            var err = source.LoadError ?? "Данные не загружены";
            if (err.Length > 22) err = err.Substring(0, 22);
            lines = new (string, ConsoleColor)[]
            {
            ("── ПОГОДА ──", ConsoleColor.DarkGray),
            ("  нет данных", ConsoleColor.DarkGray),
            (err, ConsoleColor.Red),
            };
        }
        else
        {
            var d = weather.Current;
            lines = new (string, ConsoleColor)[]
            {
            ("── ПОГОДА ──", ConsoleColor.Cyan),
            ($"Час: {weather.HourIndex:D2}", ConsoleColor.DarkGray),
            ($"Воздух:{d.AirTemp,7:F1}°C", ConsoleColor.White),
            ($"Земля: {d.GroundTemp,7:F1}°C", d.GroundTemp < 0 ? ConsoleColor.Blue : ConsoleColor.White),
            ($"Влажн: {d.AirHumidity * 100,6:F0}%", ConsoleColor.White),
            ($"Почва: {d.SoilMoisture * 100,6:F0}%", d.SoilMoisture < 0.2f ? ConsoleColor.Red : ConsoleColor.Green),
            ($"Солнц: {d.SolarEnergyMj,6:F2}МДж", ConsoleColor.Yellow),
            ($"Снег:  {d.EffectiveSnowCm,6:F1}см", ConsoleColor.White),
            ($"Осад:  {d.PrecipitationMm,6:F1}мм", ConsoleColor.Blue),
            };
        }

        for (int i = 0; i < lines.Length; i++)
        {
            var (text, color) = lines[i];
            for (int j = 0; j < text.Length && startX + j < frame.Width; j++)
            {
                if (startY + i < frame.Height)
                    Put(frame, startX + j, startY + i, text[j], color, ConsoleColor.Black);
            }
        }
    }

    // В методе DrawGameTime:
    private static void DrawGameTime(RenderFrame frame, GameState state)
    {
        // Используем CalendarHelper для правильного отображения
        string gameTimeStr = CalendarHelper.FormatGameTime(
            state.GameStartDate, state.GameTimeElapsed, state.DaysPerMonth);
        string speedStr = $"x{state.TimeSpeed:F0}";

        // Реальное время, прошедшее с запуска
        TimeSpan realTime = TimeSpan.FromSeconds(state.ElapsedTime);
        string realTimeStr = $"{(int)realTime.TotalHours:D2}:{realTime.Minutes:D2}:{realTime.Seconds:D2}";

        int startX = frame.Width - 12;  // Сдвинуто вправо
        int startY = frame.Height - 4;

        // Разделяем "dd.MM.yyyy HH:mm:ss" на дату и время
        string[] timeParts = gameTimeStr.Split(' ');
        string dateStr = timeParts.Length > 0 ? timeParts[0] : "";
        string timeOfDayStr = timeParts.Length > 1 ? timeParts[1] : "";

        // 1. Дата игры
        for (int i = 0; i < dateStr.Length && startX + i < frame.Width; i++)
            Put(frame, startX + i, startY, dateStr[i], ConsoleColor.White, ConsoleColor.Black);

        // 2. Время игры
        for (int i = 0; i < timeOfDayStr.Length && startX + i < frame.Width; i++)
            Put(frame, startX + i, startY + 1, timeOfDayStr[i], ConsoleColor.Yellow, ConsoleColor.Black);

        // 3. Скорость времени
        for (int i = 0; i < speedStr.Length && startX + i < frame.Width; i++)
            Put(frame, startX + i, startY + 2, speedStr[i], ConsoleColor.Green, ConsoleColor.Black);

        // 4. Реальное время
        for (int i = 0; i < realTimeStr.Length && startX + i < frame.Width; i++)
            Put(frame, startX + i, startY + 3, realTimeStr[i], ConsoleColor.DarkGray, ConsoleColor.Black);
    }
    private static void Put(RenderFrame frame, int x, int y, char symbol, ConsoleColor fg, ConsoleColor bg)
    {
        if ((uint)x >= (uint)frame.Width || (uint)y >= (uint)frame.Height) return;
        int index = y * frame.Width + x;
        frame.Symbols[index] = symbol;
        frame.Foreground[index] = fg;
        frame.Background[index] = bg;
    }
}