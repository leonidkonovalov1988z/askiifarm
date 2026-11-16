// Rendering/HudPanel.cs
namespace AsciiFarm.Rendering;

/// <summary>Блок HUD: заголовок + строки. Высота считается сама, угадывать нечего.</summary>
public sealed class HudPanel
{
    private readonly List<(string Text, ConsoleColor Color)> _lines = new();
    private readonly string _title;

    public HudPanel(string title) => _title = $"── {title} ──";

    /// <summary>+1 — заголовок.</summary>
    public int Height => _lines.Count + 1;

    public HudPanel Add(string text, ConsoleColor color)
    {
        _lines.Add((text ?? "", color));
        return this;
    }

    public HudPanel AddRange(IEnumerable<string> lines, ConsoleColor color)
    {
        foreach (var line in lines) Add(line, color);
        return this;
    }

    public void Draw(RenderFrame frame, int x, int y, int width)
    {
        FrameDraw.Text(frame, x, y, Fit(_title, width), ConsoleColor.Cyan);
        for (int i = 0; i < _lines.Count; i++)
        {
            var (text, color) = _lines[i];
            FrameDraw.Text(frame, x, y + 1 + i, Fit(text, width), color);
        }
    }

    private static string Fit(string s, int width) =>
        width <= 0 ? "" : (s.Length <= width ? s : s[..width]);
}