// App/GameSettings.cs
namespace AsciiFarm.App;

public sealed class GameSettings
{
    public string MapPath { get; init; } = "map_raw.txt";
    public int ScreenWidth { get; init; } = 80;
    public int ScreenHeight { get; init; } = 25;
    public int SimulationFps { get; init; } = 30;
    public int RenderFps { get; init; } = 60;
    public int InputPollMs { get; init; } = 10;
    public int CommandQueueCapacity { get; init; } = 32;
    public int PlayerStartX { get; init; } = 28;
    public int PlayerStartY { get; init; } = 12;
}