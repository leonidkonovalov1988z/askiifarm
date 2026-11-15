using AsciiFarm.Core;

namespace AsciiFarm.Components;

public struct Position
{
    public int X;
    public int Y;
    public Position(int x, int y) { X = x; Y = y; }
}

[DontSave]
public struct MoveIntent
{
    public int Dx;
    public int Dy;
    public MoveIntent(int dx, int dy) { Dx = dx; Dy = dy; }
}

public struct Glyph
{
    public char Symbol;
    public ConsoleColor Foreground;
    public ConsoleColor Background;
    public int Layer;
}

public struct PlayerTag { }

/// <summary>
/// Полные данные одной клетки карты.
/// </summary>
public struct TileData
{
    public char Symbol;
    public float Nitrogen;      // 0.0 - 1.0
    public float Phosphorus;    // 0.0 - 1.0
    public float Potassium;     // 0.0 - 1.0
    public float Moisture;      // 0.0 - 1.0
}

public struct TileMapComponent
{
    public TileData[,] Tiles;
    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);
}