// Rendering/RenderFrame.cs
namespace AsciiFarm.Rendering;

/// <summary>Данные кадра. Ни одного обработчика (очистку делает система).</summary>
public sealed class RenderFrame
{
    public int Width { get; }
    public int Height { get; }
    public char[] Symbols { get; }
    public ConsoleColor[] Foreground { get; }
    public ConsoleColor[] Background { get; }
    public long Tick { get; set; }

    public RenderFrame(int width, int height)
    {
        Width = width;
        Height = height;
        Symbols = new char[width * height];
        Foreground = new ConsoleColor[width * height];
        Background = new ConsoleColor[width * height];
    }
}