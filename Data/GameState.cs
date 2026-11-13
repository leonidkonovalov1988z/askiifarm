// Data/GameState.cs
namespace AsciiFarm.Data;

public sealed class GameState
{
    public bool IsRunning { get; set; } = true;
    public bool NeedsSave { get; set; } = false;
    public long Tick { get; set; }
    public float DeltaTime { get; set; }
    public double ElapsedTime { get; set; }
    public double TimeSpeed { get; set; }
    public double DefaultTimeSpeed { get; set; }  // НОВОЕ
    public double GameTimeElapsed { get; set; }
    public DateTime GameStartDate { get; set; }
    public double? FastForwardUntil { get; set; }
    public double MaxTimeStep { get; set; } = 3600.0;
    public int DaysPerMonth { get; set; }  // НОВОЕ
}