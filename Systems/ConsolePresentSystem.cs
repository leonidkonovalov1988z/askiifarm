// Systems/ConsolePresentSystem.cs
using AsciiFarm.Rendering;
using askiifarm;
using askiifarm.Rendering;
namespace AsciiFarm.Systems;

/// <summary>
/// Работает в потоке рендера. Метод намеренно НЕ принимает World:
/// у рендера нет и не может быть доступа к игровым данным.
/// </summary>
public sealed class ConsolePresentSystem
{
    private readonly DoubleBufferedConsole _console;
    private readonly FrameExchange<RenderFrame> _frames;

    public ConsolePresentSystem(DoubleBufferedConsole console, FrameExchange<RenderFrame> frames)
    {
        _console = console;
        _frames = frames;
    }

    public void Present()
    {
        if (!_frames.TryAcquireFresh(out var frame)) return;   // нового кадра нет — не перерисовываем

        for (int y = 0; y < frame.Height; y++)
            for (int x = 0; x < frame.Width; x++)
            {
                int i = y * frame.Width + x;
                _console.PutChar(x, y, frame.Symbols[i], frame.Foreground[i], frame.Background[i]);
            }

        _console.Draw();
    }
}