// Systems/SystemPipeline.cs
using AsciiFarm.Core;
namespace AsciiFarm.Systems;

public static class SystemPipeline
{
    public static ISystem[] CreateSimulationSystems() => new ISystem[]
    {
        new ElapsedTimeSystem(),     // 1. Время
        new FpsUpdateSystem(),       // 2. Счётчик FPS
        new PlayerIntentSystem(),    // 3. Ввод → намерения
        new MovementSystem(),        // 4. Физика
        new RiverFlowSystem(),       // 5. Анимация воды
    };
}