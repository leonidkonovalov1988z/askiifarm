// App/GameSettings.cs
namespace AsciiFarm.App;

public sealed class GameSettings
{
    public string MapPath { get; init; } = "map_raw.txt";
    public string ContentRoot { get; init; } = "Content";
    public string WeatherCsvPath { get; init; } = "Content/weather.csv";
    public int ScreenWidth { get; init; } = 110;
    public int ScreenHeight { get; init; } = 25;
    public int SimulationFps { get; init; } = 12;
    public int RenderFps { get; init; } = 24;
    public int InputPollMs { get; init; } = 10;
    public int CommandQueueCapacity { get; init; } = 32;
    public int PlayerStartX { get; init; } = 28;
    public int PlayerStartY { get; init; } = 12;
    public string PlayerCharacterKey { get; init; } = "player";
    public bool UseJsonContent { get; init; } = true;
    public double MaxTimeStep { get; init; } = 3600.0;

    // НОВОЕ: дата и время начала игры
    public DateTime GameStartDate { get; init; } = new DateTime(2000, 5, 1, 8, 0, 0);
}