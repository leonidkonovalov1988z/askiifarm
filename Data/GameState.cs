namespace AsciiFarm.Data;

public sealed class GameState
{
    public bool IsRunning { get; set; } = true;
    public bool NeedsSave { get; set; } = false;
    public long Tick { get; set; }
    public float DeltaTime { get; set; }
    public double ElapsedTime { get; set; }
}