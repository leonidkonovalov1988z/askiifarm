// Systems/ElapsedTimeSystem.cs
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Systems;

[GameSystem(Order = 1)]
public sealed class ElapsedTimeSystem : ISystem   // P2-10: stateless
{
    public void Update(World world)
    {
        var state = world.Resource<GameState>();
        var clock = world.Resource<SimulationClock>();
        var fpsCounter = world.Resource<FpsCounter>();

        double realDeltaTime = Math.Min(clock.NextDelta(), 0.1);

        state.ElapsedTime += realDeltaTime;
        state.DeltaTime = (float)realDeltaTime;
        state.Tick++;                       // единственный инкремент Tick в проекте
        fpsCounter.RecordTick();

        if (state.FastForwardUntil.HasValue) return;   // время уже добавлено в SimulationLoop

        state.GameTimeElapsed += Math.Min(realDeltaTime * state.TimeSpeed, state.MaxTimeStep);
    }
}