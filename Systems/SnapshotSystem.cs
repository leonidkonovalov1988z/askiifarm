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

    private static void DrawUI(RenderFrame frame, GameState state, FpsCounter fpsCounter)
    {
        var elapsed = TimeSpan.FromSeconds(state.ElapsedTime);
        string timeStr = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        string fpsStr = $"FPS: {fpsCounter.CurrentFps:F0}";

        int startX = frame.Width - Math.Max(timeStr.Length, fpsStr.Length) - 1;

        for (int i = 0; i < timeStr.Length; i++)
            if (startX + i < frame.Width)
                Put(frame, startX + i, 0, timeStr[i], ConsoleColor.Yellow, ConsoleColor.Black);

        for (int i = 0; i < fpsStr.Length; i++)
            if (startX + i < frame.Width && 1 < frame.Height)
                Put(frame, startX + i, 1, fpsStr[i], ConsoleColor.Green, ConsoleColor.Black);
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