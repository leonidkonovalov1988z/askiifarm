// App/SimulationLoop.cs
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Input;
using AsciiFarm.Rendering;
using AsciiFarm.Systems;
namespace AsciiFarm.App;

public sealed class SimulationLoop
{
    private readonly World _world;
    private readonly CommandChannel _channel;
    private readonly ISystem[] _systems;
    private readonly SnapshotSystem _snapshot;
    private readonly FrameExchange<RenderFrame> _frames;
    private readonly CancellationTokenSource _shutdown;

    public SimulationLoop(World world, CommandChannel channel, ISystem[] systems,
                          SnapshotSystem snapshot, FrameExchange<RenderFrame> frames,
                          CancellationTokenSource shutdown)
    {
        _world = world; _channel = channel; _systems = systems;
        _snapshot = snapshot; _frames = frames; _shutdown = shutdown;
    }

    public void Run(CancellationToken token)
    {
        _world.Guard.AttachToCurrentThread();
        _frames.AttachWriter();

        var state = _world.Resource<GameState>();
        var buffer = _world.Resource<CommandBuffer>();
        var limiter = _world.Resource<FpsLimiter>();

        while (!token.IsCancellationRequested && state.IsRunning)
        {
            while (!limiter.BeginTick())
            {
                Thread.Sleep(1);
                if (token.IsCancellationRequested || !state.IsRunning) break;
            }

            if (token.IsCancellationRequested || !state.IsRunning) break;

            using (_world.Guard.Enter())
            {
                _channel.DrainTo(buffer.Items);
                foreach (var system in _systems) system.Update(_world);
                _snapshot.Update(_world);
            }
        }

        _shutdown.Cancel();
    }
}