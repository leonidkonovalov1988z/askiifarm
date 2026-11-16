// Rendering/HudRenderer.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Rendering;

public sealed class HudRenderer : IFrameRenderer
{
    private const int ColWidth = 20;
    private const int Gap = 2;

    public void Render(RenderFrame frame, World world)
    {
        int totalWidth = ColWidth * 2 + Gap;
        if (frame.Width < totalWidth + 5) return;

        int startX = frame.Width - totalWidth;
        int col1 = startX;
        int col2 = startX + ColWidth + Gap;

        DrawDateTime(frame, col1, 0, world.Resource<GameState>());
        DrawTileInfo(frame, world, col1, Math.Max(0, frame.Height - 24));
        DrawStats(frame, col2, 0, world.Resource<FpsCounter>());
        DrawWeather(frame, col2, 3, world);
        DrawFarm(frame, col2, 13, world);
    }

    private static void DrawDateTime(RenderFrame f, int x, int y, GameState state)
    {
        string gameTime = CalendarHelper.FormatGameTime(state.GameStartDate, state.GameTimeElapsed, state.DaysPerMonth);
        var parts = gameTime.Split(' ');
        var real = TimeSpan.FromSeconds(state.ElapsedTime);

        FrameDraw.Text(f, x, y++, "── ВРЕМЯ ──", ConsoleColor.Cyan);
        FrameDraw.Text(f, x, y++, parts.Length > 0 ? parts[0] : "", ConsoleColor.White);
        FrameDraw.Text(f, x, y++, parts.Length > 1 ? parts[1] : "", ConsoleColor.Yellow);
        FrameDraw.Text(f, x, y++, $"Скорость: x{state.TimeSpeed:F0}", ConsoleColor.Green);
        FrameDraw.Text(f, x, y, $"Реальное: {(int)real.TotalHours:D2}:{real.Minutes:D2}:{real.Seconds:D2}", ConsoleColor.DarkGray);
    }

    private static void DrawTileInfo(RenderFrame f, World world, int x, int y)
    {
        var players = world.Query<PlayerTag, Position>();
        var maps = world.Query<TileMapComponent>();
        if (players.Length == 0 || maps.Length == 0) return;

        var map = world.Store<TileMapComponent>().Get(maps[0]);
        var pos = world.Store<Position>().Get(players[0]);
        if (pos.X < 0 || pos.Y < 0 || pos.X >= map.Width || pos.Y >= map.Height) return;

        char tile = map.Tiles[pos.X, pos.Y].Symbol;
        FrameDraw.Text(f, x, y++, "── ТАЙЛ ──", ConsoleColor.Cyan);
        FrameDraw.Text(f, x, y++, $"Позиция: ({pos.X}, {pos.Y})", ConsoleColor.White);

        // без ToList(): строки идут потоком
        foreach (var line in world.Resource<TileInfoRegistry>().GetAllInfo(pos.X, pos.Y, tile, world))
        {
            if (y >= f.Height) break;
            FrameDraw.Text(f, x, y++, line, ConsoleColor.Gray);
        }
    }

    private static void DrawStats(RenderFrame f, int x, int y, FpsCounter fps)
    {
        FrameDraw.Text(f, x, y++, "── СТАТИСТИКА ──", ConsoleColor.Cyan);
        FrameDraw.Text(f, x, y++, $"FPS: {fps.RealFps}", ConsoleColor.Green);
        FrameDraw.Text(f, x, y, $"TPS: {fps.TicksPerSecond}", ConsoleColor.Cyan);
    }

    private static void DrawWeather(RenderFrame f, int x, int y, World world)
    {
        var weather = world.Resource<WeatherCurrent>();
        var source = world.Resource<WeatherDataSource>();

        if (!weather.HasData)
        {
            FrameDraw.Text(f, x, y++, "── ПОГОДА ──", ConsoleColor.DarkGray);
            FrameDraw.Text(f, x, y++, "нет данных", ConsoleColor.DarkGray);
            if (source.LoadError != null) FrameDraw.Text(f, x, y, source.LoadError, ConsoleColor.Red);
            return;
        }

        var d = weather.Current;
        FrameDraw.Text(f, x, y++, "── ПОГОДА ──", ConsoleColor.Cyan);
        FrameDraw.Text(f, x, y++, $"Час: {weather.HourIndex:D2}", ConsoleColor.DarkGray);
        FrameDraw.Text(f, x, y++, $"Воздух: {d.AirTemp,7:F1}°C", ConsoleColor.White);
        FrameDraw.Text(f, x, y++, $"Земля: {d.GroundTemp,7:F1}°C", d.GroundTemp < 0 ? ConsoleColor.Blue : ConsoleColor.White);
        FrameDraw.Text(f, x, y++, $"Влажн: {d.AirHumidity * 100,6:F0}%", ConsoleColor.White);
        FrameDraw.Text(f, x, y++, $"Почва: {d.SoilMoisture * 100,6:F0}%", d.SoilMoisture < 0.2f ? ConsoleColor.Red : ConsoleColor.Green);
        FrameDraw.Text(f, x, y++, $"Солнц: {d.SolarEnergyMj,6:F2}МДж", ConsoleColor.Yellow);
        FrameDraw.Text(f, x, y++, $"Снег: {d.EffectiveSnowCm,6:F1}см", ConsoleColor.White);
        FrameDraw.Text(f, x, y, $"Осад: {d.PrecipitationMm,6:F1}мм", ConsoleColor.Blue);
    }

    private static string Fit(string s) => s.Length <= ColWidth ? s : s[..ColWidth];

    private static void DrawFarm(RenderFrame f, int x, int y, World world)
    {
        var catalog = world.Resource<CropCatalog>();
        var selection = world.Resource<FarmSelection>();
        var state = world.Resource<GameState>();

        FrameDraw.Text(f, x, y++, "── ПОСЕВ ──", ConsoleColor.Cyan);

        var crop = catalog.ByIndex(selection.Index);
        if (crop == null)
        {
            FrameDraw.Text(f, x, y++, "нет культур", ConsoleColor.DarkGray);
            if (catalog.LoadError != null) FrameDraw.Text(f, x, y, Fit(catalog.LoadError), ConsoleColor.Red);
            return;
        }

        float seeds = 0f, veg = 0f, fiber = 0f;
        var players = world.Query<PlayerTag, GranaryComponent>();
        if (players.Length > 0)
        {
            var g = world.Store<GranaryComponent>().Get(players[0]);
            seeds = Granary.Get(g, crop.Id);
            veg = Granary.Get(g, Granary.Vegetables);
            fiber = Granary.Get(g, Granary.Fiber);
        }

        FrameDraw.Text(f, x, y++, Fit($"> {crop.Name}"), ConsoleColor.White);
        FrameDraw.Text(f, x, y++, $"Семена: {seeds:F1}", ConsoleColor.Green);
        FrameDraw.Text(f, x, y++, $"Овощи: {veg:F1}", ConsoleColor.Green);
        FrameDraw.Text(f, x, y++, $"Волокно: {fiber:F1}", ConsoleColor.Cyan);

        var msg = world.Resource<FarmLog>().Current(state.ElapsedTime);
        if (msg != null) FrameDraw.Text(f, x, y, Fit(msg), ConsoleColor.Yellow);
    }
}