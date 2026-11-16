// App/RenderLoop.cs
using AsciiFarm.Rendering;
using AsciiFarm.Systems;

namespace AsciiFarm.App;

public sealed class RenderLoop
{
    private readonly DoubleBufferedConsole _console;
    private readonly FrameExchange<RenderFrame> _frames;
    private readonly ConsolePresentSystem _present;
    private readonly int _frameMs;

    // P1-6: никаких Console.Error.WriteLine — альтернативный буфер этого не прощает.
    public RenderLoop(DoubleBufferedConsole console, FrameExchange<RenderFrame> frames,
                      ConsolePresentSystem present, int frameMs)
    {
        _console = console;
        _frames = frames;
        _present = present;
        _frameMs = frameMs;
    }

    public void Run(CancellationToken token)
    {
        _console.AttachToCurrentThread();
        _frames.AttachReader();
        var (lastWidth, lastHeight) = _console.GetWindowSize();

        while (!token.IsCancellationRequested)
        {
            var (w, h) = _console.GetWindowSize();
            if (w != lastWidth || h != lastHeight)
            {
                lastWidth = w; lastHeight = h;
                int nw = w, nh = h;
                _console.Resize(nw, nh);
                _frames.Resize(() => new RenderFrame(nw, nh));   // применит писатель
            }

            _present.Present();
            Thread.Sleep(_frameMs);
        }
    }
}