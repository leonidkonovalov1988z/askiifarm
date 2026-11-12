// App/SimulationLoop.cs
using System.Diagnostics;
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
    private readonly int _tickMs;
    private readonly CancellationTokenSource _shutdown;

    public SimulationLoop(World world, CommandChannel channel, ISystem[] systems,
                          SnapshotSystem snapshot, FrameExchange<RenderFrame> frames,
                          int tickMs, CancellationTokenSource shutdown)
    {
        _world = world; _channel = channel; _systems = systems;
        _snapshot = snapshot; _frames = frames; _tickMs = tickMs; _shutdown = shutdown;
    }

    public void Run(CancellationToken token)
    {
        _world.Guard.AttachToCurrentThread();   // с этой минуты World — только наш
        _frames.AttachWriter();

        var state = _world.Resource<GameState>();
        var buffer = _world.Resource<CommandBuffer>();
        var clock = Stopwatch.StartNew();

        while (!token.IsCancellationRequested && state.IsRunning)
        {
            long startedAt = clock.ElapsedMilliseconds;

            using (_world.Guard.Enter())        // блокиратор «данные изменяются»
            {
                _channel.DrainTo(buffer.Items); // единственная точка чтения чужих данных
                foreach (var system in _systems) system.Update(_world);
                state.Tick++;
                _snapshot.Update(_world);       // публикация кадра
            }

            int delay = _tickMs - (int)(clock.ElapsedMilliseconds - startedAt);
            if (delay > 0) Thread.Sleep(delay);
        }

        _shutdown.Cancel();                     // гасим ввод и рендер
    }
}