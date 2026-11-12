// App/RenderLoop.cs
using AsciiFarm.Rendering;
using AsciiFarm.Systems;
using askiifarm;
using askiifarm.Rendering;
namespace AsciiFarm.App;

public sealed class RenderLoop
{
    private readonly DoubleBufferedConsole _console;
    private readonly FrameExchange<RenderFrame> _frames;
    private readonly ConsolePresentSystem _present;
    private readonly int _frameMs;

    public RenderLoop(DoubleBufferedConsole console, FrameExchange<RenderFrame> frames,
                      ConsolePresentSystem present, int frameMs)
    {
        _console = console; _frames = frames; _present = present; _frameMs = frameMs;
    }

    public void Run(CancellationToken token)
    {
        _console.AttachToCurrentThread();       // консоль — только наша
        _frames.AttachReader();

        while (!token.IsCancellationRequested)
        {
            _present.Present();
            Thread.Sleep(_frameMs);
        }
    }
}