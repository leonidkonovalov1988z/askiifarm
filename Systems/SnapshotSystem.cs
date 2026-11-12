// Systems/SnapshotSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Rendering;
namespace AsciiFarm.Systems;

/// <summary>
/// Работает в потоке симуляции: читает World, пишет в СВОЙ write-буфер и публикует его.
/// Заменяет GameRenderer, но ничего не знает о консоли.
/// </summary>
public sealed class SnapshotSystem
{
    private readonly FrameExchange<RenderFrame> _frames;

    public SnapshotSystem(FrameExchange<RenderFrame> frames) => _frames = frames;

    public void Update(World world)
    {
        var frame = _frames.WriteBuffer;
        var palette = world.Resource<TilePalette>();

        Array.Fill(frame.Symbols, ' ');
        Array.Fill(frame.Foreground, palette.Fallback.Foreground);
        Array.Fill(frame.Background, palette.Fallback.Background);

        foreach (var entity in world.Query<TileMapComponent>())
            DrawTiles(frame, world.Store<TileMapComponent>().Get(entity), palette);

        var glyphs = world.Store<Glyph>();
        var positions = world.Store<Position>();
        foreach (var entity in world.Query<Glyph, Position>().OrderBy(e => glyphs.Get(e).Layer))
        {
            var glyph = glyphs.Get(entity);
            var pos = positions.Get(entity);
            Put(frame, pos.X, pos.Y, glyph.Symbol, glyph.Foreground, glyph.Background);
        }

        frame.Tick = world.Resource<GameState>().Tick;
        _frames.Publish();
    }

    private static void DrawTiles(RenderFrame frame, TileMapComponent map, TilePalette palette)
    {
        int width = Math.Min(map.Width, frame.Width);
        int height = Math.Min(map.Height, frame.Height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var style = palette.Resolve(map.Tiles[x, y]);
                Put(frame, x, y, map.Tiles[x, y], style.Foreground, style.Background);
            }
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