// Systems/ElapsedTimeSystem.cs
using AsciiFarm.Core;
using AsciiFarm.Data;
using System.Diagnostics;
namespace AsciiFarm.Systems;

[GameSystem(Order = 1)]
public sealed class ElapsedTimeSystem : ISystem
{
    private readonly Stopwatch _tickTimer = Stopwatch.StartNew();
    private double _lastTickTime = 0;

    public void Update(World world)
    {
        var state = world.Resource<GameState>();
        var fpsCounter = world.Resource<FpsCounter>();

        // Считаем тики всегда (для корректного TPS)
        fpsCounter.RecordTick();
        state.Tick++;

        // При перемотке время обновляется вручную в SimulationLoop
        if (state.FastForwardUntil.HasValue) return;

        double now = _tickTimer.Elapsed.TotalSeconds;
        double realDeltaTime = now - _lastTickTime;
        _lastTickTime = now;
        realDeltaTime = Math.Min(realDeltaTime, 0.1);

        // Игровое время с учётом скорости, но не более MaxTimeStep
        double effectiveDeltaTime = realDeltaTime * state.TimeSpeed;
        effectiveDeltaTime = Math.Min(effectiveDeltaTime, state.MaxTimeStep);

        state.DeltaTime = (float)realDeltaTime;
        state.ElapsedTime += realDeltaTime;
        state.GameTimeElapsed += effectiveDeltaTime;
    }
}