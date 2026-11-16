// Rendering/MapRenderer.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Systems;

namespace AsciiFarm.Rendering;

public sealed class MapRenderer : IFrameRenderer
{
    public void Render(RenderFrame frame, World world)
    {
        var palette = world.Resource<TilePalette>();
        world.TryGetResource<WaterColorCache>(out var water);   // P1-8

        foreach (var entity in world.Query<TileMapComponent>())
        {
            var map = world.Store<TileMapComponent>().Get(entity);
            int width = Math.Min(map.Width, frame.Width);
            int height = Math.Min(map.Height, frame.Height);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    char tile = map.Tiles[x, y].Symbol;
                    var style = water != null && water.Colors.TryGetValue((x, y), out var ws)
                        ? ws
                        : palette.Resolve(tile);
                    FrameDraw.Put(frame, x, y, tile, style.Foreground, style.Background);
                }
        }
    }
}