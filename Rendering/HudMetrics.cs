// Rendering/HudMetrics.cs
namespace AsciiFarm.Rendering;

/// <summary>Геометрия HUD. Все координаты — от ЛЕВОГО ВЕРХНЕГО угла кадра.</summary>
public sealed class HudMetrics
{
    public int OriginX { get; set; } = 66;      // левый край HUD (не зависит от ширины окна)
    public int OriginY { get; set; } = 0;
    public int ColumnWidth { get; set; } = 21;  // ширина колонки; строки обрезаются по ней
    public int ColumnGap { get; set; } = 2;
    public int RowGap { get; set; } = 1;
}