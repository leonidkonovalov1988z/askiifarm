// Rendering/ConsolePresentSystem.cs
using AsciiFarm.Data;
using AsciiFarm.Rendering;
using askiifarm;
using askiifarm.Rendering;
namespace AsciiFarm.Systems;

public sealed class ConsolePresentSystem
{
    private readonly DoubleBufferedConsole _console;
    private readonly FrameExchange<RenderFrame> _frames;
    private readonly FpsCounter _fpsCounter;  // ДОБАВИТЬ

    public ConsolePresentSystem(DoubleBufferedConsole console, FrameExchange<RenderFrame> frames,
                                FpsCounter fpsCounter)  // ДОБАВИТЬ ПАРАМЕТР
    {
        _console = console;
        _frames = frames;
        _fpsCounter = fpsCounter;  // ДОБАВИТЬ
    }

    public void Present()
    {
        if (!_frames.TryAcquireFresh(out var frame)) return;

        for (int y = 0; y < frame.Height; y++)
            for (int x = 0; x < frame.Width; x++)
            {
                int i = y * frame.Width + x;
                _console.PutChar(x, y, frame.Symbols[i], frame.Foreground[i], frame.Background[i]);
            }

        _console.Draw();
        _fpsCounter.RecordFrame(frame.Tick);  // ДОБАВИТЬ
    }
}