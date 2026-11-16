// Rendering/HudLayout.cs
namespace AsciiFarm.Rendering;

/// <summary>
/// Раскладка панелей: колонки слева направо, внутри колонки — сверху вниз.
/// Шаг по Y = реальная высота панели, поэтому перекрытия невозможны.
/// </summary>
public static class HudLayout
{
    public static void Draw(RenderFrame frame, HudMetrics m, params HudPanel[][] columns)
    {
        int x = m.OriginX;
        foreach (var column in columns)
        {
            if (x >= frame.Width) return;
            int y = m.OriginY;
            foreach (var panel in column)
            {
                if (y >= frame.Height) break;
                panel.Draw(frame, x, y, m.ColumnWidth);
                y += panel.Height + m.RowGap;
            }
            x += m.ColumnWidth + m.ColumnGap;
        }
    }
}