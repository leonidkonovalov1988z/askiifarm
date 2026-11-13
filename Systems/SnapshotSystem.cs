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
        try { waterCache = world.Resource<WaterColorCache>(); } catch { /* первый кадр */ }

        Array.Fill(frame.Symbols, ' ');
        Array.Fill(frame.Foreground, palette.Fallback.Foreground);
        Array.Fill(frame.Background, palette.Fallback.Background);

        foreach (var entity in world.Query<TileMapComponent>())
            DrawTiles(frame, world.Store<TileMapComponent>().Get(entity), palette, waterCache);

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

        DrawUI(frame, gameState, fpsCounter);
        DrawWeatherUI(frame, world.Resource<WeatherCurrent>(), world.Resource<WeatherDataSource>());
        DrawGameTime(frame, gameState);
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
                char tile = map.Tiles[x, y];
                var style = waterCache != null && waterCache.Colors.TryGetValue((x, y), out var ws)
                    ? ws
                    : palette.Resolve(tile);

                Put(frame, x, y, tile, style.Foreground, style.Background);
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
        // Игровое время = GameStartDate + GameTimeElapsed
        DateTime gameTime = state.GameStartDate.AddSeconds(state.GameTimeElapsed);
        string dateStr = gameTime.ToString("dd.MM.yyyy");
        string timeStr = gameTime.ToString("HH:mm:ss");
        string speedStr = $"x{state.TimeSpeed:F0}";

        // Реальное время, прошедшее с запуска
        TimeSpan realTime = TimeSpan.FromSeconds(state.ElapsedTime);
        string realTimeStr = $"{(int)realTime.TotalHours:D2}:{realTime.Minutes:D2}:{realTime.Seconds:D2}";

        int startX = frame.Width - 12;  // Сдвинуто вправо
        int startY = frame.Height - 3;

        for (int i = 0; i < dateStr.Length && startX + i < frame.Width; i++)
            Put(frame, startX + i, startY, dateStr[i], ConsoleColor.White, ConsoleColor.Black);
        for (int i = 0; i < timeStr.Length && startX + i < frame.Width; i++)
            Put(frame, startX + i, startY + 1, timeStr[i], ConsoleColor.Yellow, ConsoleColor.Black);
        for (int i = 0; i < speedStr.Length && startX + i < frame.Width; i++)
            Put(frame, startX + i, startY + 2, speedStr[i], ConsoleColor.Green, ConsoleColor.Black);
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