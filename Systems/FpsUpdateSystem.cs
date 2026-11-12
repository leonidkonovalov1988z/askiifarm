// Systems/FpsUpdateSystem.cs
using AsciiFarm.Core;
using AsciiFarm.Data;
namespace AsciiFarm.Systems;

/// <summary>Записывает замер FPS каждый тик.</summary>
public sealed class FpsUpdateSystem : ISystem
{
    public void Update(World world)
    {
        var fpsCounter = world.Resource<FpsCounter>();
        var limiter = world.Resource<FpsLimiter>();

        float instantFps = limiter.DeltaTime > 0 ? 1.0f / limiter.DeltaTime : 0;
        fpsCounter.RecordFrame(limiter.ElapsedTime);
        fpsCounter.UpdateAverage(instantFps);
    }
}