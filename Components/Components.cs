// Components/Components.cs
namespace AsciiFarm.Components;

public struct Position
{
    public int X;
    public int Y;
    public Position(int x, int y) { X = x; Y = y; }
}

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
    public int Layer;           // 0 — карта, 10 — акторы, 20 — UI
}

/// <summary>Маркер «управляется игроком». Данных нет.</summary>
public struct PlayerTag { }

public struct TileMapComponent
{
    public char[,] Tiles;
    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);
}