// Systems/SystemPipeline.cs
using AsciiFarm.Core;
namespace AsciiFarm.Systems;

public static class SystemPipeline
{
    public static ISystem[] CreateSimulationSystems() => new ISystem[]
    {
        new PlayerIntentSystem(),
        new MovementSystem(),
        // сюда позже: GrowthSystem, HungerSystem, AISystem... без правок остальных файлов
    };
}