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

        // Реальное время между тиками — считаем всегда
        double now = _tickTimer.Elapsed.TotalSeconds;
        double realDeltaTime = now - _lastTickTime;
        _lastTickTime = now;
        realDeltaTime = Math.Min(realDeltaTime, 0.1);

        // Реальное время всегда растёт на реальное время между тиками
        state.ElapsedTime += realDeltaTime;
        state.DeltaTime = (float)realDeltaTime;
        state.Tick++;
        fpsCounter.RecordTick();

        // При перемотке игровое время уже добавлено в SimulationLoop — не дублируем
        if (state.FastForwardUntil.HasValue) return;

        // В обычном режиме: игровое время = реальное × скорость, но не более MaxTimeStep
        double effectiveDeltaTime = realDeltaTime * state.TimeSpeed;
        effectiveDeltaTime = Math.Min(effectiveDeltaTime, state.MaxTimeStep);
        state.GameTimeElapsed += effectiveDeltaTime;
    }
}