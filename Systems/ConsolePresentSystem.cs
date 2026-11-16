// Systems/ConsolePresentSystem.cs
using AsciiFarm.Data;
using AsciiFarm.Rendering;
using System.Diagnostics;

namespace AsciiFarm.Systems;

public sealed class ConsolePresentSystem
{
    private readonly DoubleBufferedConsole _console;
    private readonly FrameExchange<RenderFrame> _frames;
    private readonly FpsCounter _fpsCounter;
    private readonly Stopwatch _clock = Stopwatch.StartNew();   // P0-3: реальное время

    public ConsolePresentSystem(DoubleBufferedConsole console,
                                FrameExchange<RenderFrame> frames,
                                FpsCounter fpsCounter)
    {
        _console = console;
        _frames = frames;
        _fpsCounter = fpsCounter;
    }

    public void Present()
    {
        if (_frames.TryAcquireFresh(out var frame))
        {
            for (int y = 0; y < frame.Height; y++)
                for (int x = 0; x < frame.Width; x++)
                {
                    int i = y * frame.Width + x;
                    _console.PutChar(x, y, frame.Symbols[i], frame.Foreground[i], frame.Background[i]);
                }

            _fpsCounter.RecordFrame(_clock.Elapsed.TotalSeconds);  // не frame.Tick!
        }
        _console.Draw();
    }
}