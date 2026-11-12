// Data/GameState.cs
namespace AsciiFarm.Data;

/// <summary>Читает/пишет только поток симуляции. Остальные видят CancellationToken.</summary>
public sealed class GameState
{
    public bool IsRunning { get; set; } = true;
    public long Tick { get; set; }
}