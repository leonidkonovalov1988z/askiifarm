// Data/TilePalette.cs
namespace AsciiFarm.Data;

public readonly record struct TileStyle(ConsoleColor Foreground, ConsoleColor Background, bool BlocksMovement);

/// <summary>Одна таблица вместо двух switch в рендере и физике.</summary>
public sealed class TilePalette
{
    public Dictionary<char, TileStyle> Tiles { get; } = new();
    public TileStyle Fallback { get; set; }
}