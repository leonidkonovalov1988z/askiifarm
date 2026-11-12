// Systems/ElapsedTimeSystem.cs
using AsciiFarm.Core;
using AsciiFarm.Data;
namespace AsciiFarm.Systems;

/// <summary>Накапливает игровое время из дельты.</summary>
public sealed class ElapsedTimeSystem : ISystem
{
    public void Update(World world)
    {
        var state = world.Resource<GameState>();
        var limiter = world.Resource<FpsLimiter>();

        state.DeltaTime = limiter.DeltaTime;
        state.ElapsedTime += limiter.DeltaTime;
        state.Tick++;
    }
}