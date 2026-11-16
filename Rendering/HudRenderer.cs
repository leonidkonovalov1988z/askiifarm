// Rendering/HudRenderer.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Rendering;

public sealed class HudRenderer : IFrameRenderer
{
    private static readonly HudMetrics DefaultMetrics = new();

    public void Render(RenderFrame frame, World world)
    {
        world.TryGetResource<HudMetrics>(out var metrics);
        var m = metrics ?? DefaultMetrics;
        if (frame.Width <= m.OriginX || frame.Height <= m.OriginY) return;

        var state = world.Resource<GameState>();

        // Панели с переменной высотой — ПОСЛЕДНИЕ в колонке, чтобы ничего не «прыгало».
        HudLayout.Draw(frame, m,
            new[] { TimePanel(state), TilePanel(world) },
            new[] { StatsPanel(world.Resource<FpsCounter>()), WeatherPanel(world), FarmPanel(world, state) });
    }

    private static HudPanel TimePanel(GameState state)
    {
        var p = new HudPanel("ВРЕМЯ");
        string gameTime = CalendarHelper.FormatGameTime(
            state.GameStartDate, state.GameTimeElapsed, state.DaysPerMonth);
        var parts = gameTime.Split(' ');
        var real = TimeSpan.FromSeconds(state.ElapsedTime);

        p.Add(parts.Length > 0 ? parts[0] : "", ConsoleColor.White);
        p.Add(parts.Length > 1 ? parts[1] : "", ConsoleColor.Yellow);
        p.Add($"Скорость: x{state.TimeSpeed:F0}", ConsoleColor.Green);
        p.Add($"Реальное: {(int)real.TotalHours:D2}:{real.Minutes:D2}:{real.Seconds:D2}", ConsoleColor.DarkGray);
        return p;
    }

    private static HudPanel TilePanel(World world)
    {
        var p = new HudPanel("ТАЙЛ");
        var players = world.Query<PlayerTag, Position>();
        var maps = world.Query<TileMapComponent>();
        if (players.Length == 0 || maps.Length == 0) return p.Add("нет данных", ConsoleColor.DarkGray);

        var map = world.Store<TileMapComponent>().Get(maps[0]);
        var pos = world.Store<Position>().Get(players[0]);
        if (pos.X < 0 || pos.Y < 0 || pos.X >= map.Width || pos.Y >= map.Height)
            return p.Add("вне карты", ConsoleColor.DarkGray);

        char tile = map.Tiles[pos.X, pos.Y].Symbol;
        p.Add($"Позиция: ({pos.X}, {pos.Y})", ConsoleColor.White);
        p.AddRange(world.Resource<TileInfoRegistry>().GetAllInfo(pos.X, pos.Y, tile, world), ConsoleColor.Gray);
        return p;
    }

    private static HudPanel StatsPanel(FpsCounter fps)
    {
        var p = new HudPanel("СТАТИСТИКА");
        p.Add($"FPS: {fps.RealFps}", ConsoleColor.Green);
        p.Add($"TPS: {fps.TicksPerSecond}", ConsoleColor.Cyan);
        return p;
    }

    private static HudPanel WeatherPanel(World world)
    {
        var p = new HudPanel("ПОГОДА");
        var weather = world.Resource<WeatherCurrent>();
        var source = world.Resource<WeatherDataSource>();

        if (!weather.HasData)
        {
            p.Add("нет данных", ConsoleColor.DarkGray);
            if (source.LoadError != null) p.Add(source.LoadError, ConsoleColor.Red);
            return p;
        }

        var d = weather.Current;
        p.Add($"Час: {weather.HourIndex:D2}", ConsoleColor.DarkGray);
        p.Add($"Воздух: {d.AirTemp,7:F1}°C", ConsoleColor.White);
        p.Add($"Земля: {d.GroundTemp,7:F1}°C", d.GroundTemp < 0 ? ConsoleColor.Blue : ConsoleColor.White);
        p.Add($"Влажн: {d.AirHumidity * 100,6:F0}%", ConsoleColor.White);
        p.Add($"Почва: {d.SoilMoisture * 100,6:F0}%", d.SoilMoisture < 0.2f ? ConsoleColor.Red : ConsoleColor.Green);
        p.Add($"Солнц: {d.SolarEnergyMj,6:F2}МДж", ConsoleColor.Yellow);
        p.Add($"Снег: {d.EffectiveSnowCm,6:F1}см", ConsoleColor.White);
        p.Add($"Осад: {d.PrecipitationMm,6:F1}мм", ConsoleColor.Blue);
        return p;
    }

    private static HudPanel FarmPanel(World world, GameState state)
    {
        var p = new HudPanel("ПОСЕВ");
        var catalog = world.Resource<CropCatalog>();
        var selection = world.Resource<FarmSelection>();
        var crop = catalog.ByIndex(selection.Index);

        if (crop == null)
        {
            p.Add("нет культур", ConsoleColor.DarkGray);
            if (catalog.LoadError != null) p.Add(catalog.LoadError, ConsoleColor.Red);
            return p;
        }

        float seeds = 0f, roots = 0f, veg = 0f, fiber = 0f;
        var players = world.Query<PlayerTag, GranaryComponent>();
        if (players.Length > 0)
        {
            var g = world.Store<GranaryComponent>().Get(players[0]);
            seeds = Granary.Get(g, crop.Id);
            roots = Granary.Get(g, Granary.Root(crop.Id));
            veg = Granary.Get(g, Granary.Vegetables);
            fiber = Granary.Get(g, Granary.Fiber);
        }

        p.Add($"> {crop.Name}", ConsoleColor.White);
        p.Add($"Семена: {seeds:F1}", ConsoleColor.Green);
        if (crop.Mode == HarvestMode.Biennial)
            p.Add($"На семена: {roots:F0}", ConsoleColor.DarkGreen);
        p.Add($"Овощи: {veg:F1}", ConsoleColor.Green);
        p.Add($"Волокно: {fiber:F1}", ConsoleColor.Cyan);

        var msg = world.Resource<FarmLog>().Current(state.ElapsedTime);
        if (msg != null) p.Add(msg, ConsoleColor.Yellow);
        return p;
    }
}