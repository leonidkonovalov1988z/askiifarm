namespace AsciiFarm.Data;

public sealed class GameState
{
    public bool IsRunning { get; set; } = true;
    public bool NeedsSave { get; set; } = false;
    public long Tick { get; set; }
    public float DeltaTime { get; set; }
    public double ElapsedTime { get; set; }

    // Новое: коэффициент скорости времени (1.0 = реальное время)
    public float TimeSpeed { get; set; } = 1.0f;

    // Новое: суммарное игровое время (секунды), учитывает TimeSpeed
    public double GameTimeElapsed { get; set; }

    // Новое: дата начала игры
    public DateTime GameStartDate { get; set; } = DateTime.Now;

    // Новое: промотка времени
    public double? FastForwardUntil { get; set; }
    public double MaxTimeStep { get; set; } = 3600.0;  // Максимальный шаг времени (1 час)

}