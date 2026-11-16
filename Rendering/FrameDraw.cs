// Rendering/FrameDraw.cs
namespace AsciiFarm.Rendering;

/// <summary>Общие примитивы рисования в кадр. Всё с обрезкой по границам.</summary>
public static class FrameDraw
{
    public static void Put(RenderFrame f, int x, int y, char symbol, ConsoleColor fg, ConsoleColor bg)
    {
        if ((uint)x >= (uint)f.Width || (uint)y >= (uint)f.Height) return;
        int i = y * f.Width + x;
        f.Symbols[i] = symbol;
        f.Foreground[i] = fg;
        f.Background[i] = bg;
    }

    public static void Text(RenderFrame f, int x, int y, string text, ConsoleColor fg,
                            ConsoleColor bg = ConsoleColor.Black)
    {
        for (int i = 0; i < text.Length; i++) Put(f, x + i, y, text[i], fg, bg);
    }

    public static void Clear(RenderFrame f, char symbol, ConsoleColor fg, ConsoleColor bg)
    {
        Array.Fill(f.Symbols, symbol);
        Array.Fill(f.Foreground, fg);
        Array.Fill(f.Background, bg);
    }
}