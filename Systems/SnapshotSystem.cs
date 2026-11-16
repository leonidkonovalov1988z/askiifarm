// Systems/SnapshotSystem.cs — только оркестрация.
// Мёртвые DrawUI / DrawWeatherUI / DrawGameTime удалены.
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Rendering;

namespace AsciiFarm.Systems;

public sealed class SnapshotSystem
{
    private readonly FrameExchange<RenderFrame> _frames;
    private readonly IFrameRenderer[] _renderers;

    public SnapshotSystem(FrameExchange<RenderFrame> frames, params IFrameRenderer[] renderers)
    {
        _frames = frames;
        _renderers = renderers;
    }

    public void Update(World world)
    {
        var frame = _frames.WriteBuffer;
        var fallback = world.Resource<TilePalette>().Fallback;
        FrameDraw.Clear(frame, ' ', fallback.Foreground, fallback.Background);

        foreach (var renderer in _renderers) renderer.Render(frame, world);

        frame.Tick = world.Resource<GameState>().Tick;
        _frames.Publish();
    }
}