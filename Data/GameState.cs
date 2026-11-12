// Data/GameState.cs
namespace AsciiFarm.Data;

public sealed class GameState
{
    public bool IsRunning { get; set; } = true;
    public long Tick { get; set; }
    public float DeltaTime { get; set; }
    public double ElapsedTime { get; set; }
}