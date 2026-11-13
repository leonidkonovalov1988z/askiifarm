// Systems/FpsUpdateSystem.cs
using AsciiFarm.Core;
using AsciiFarm.Data;
namespace AsciiFarm.Systems;

/// <summary>Записывает замер FPS каждый тик.</summary>
[GameSystem(Order = 2)]
public sealed class FpsUpdateSystem : ISystem
{
    public void Update(World world)
    {
        var fpsCounter = world.Resource<FpsCounter>();
        var limiter = world.Resource<FpsLimiter>();

        float instantFps = limiter.DeltaTime > 0 ? 1.0f / limiter.DeltaTime : 0;
        
        fpsCounter.UpdateAverage(instantFps);
    }
}