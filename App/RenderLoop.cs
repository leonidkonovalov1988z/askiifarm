using AsciiFarm.Rendering;
using AsciiFarm.Systems;
using askiifarm.Rendering;

namespace AsciiFarm.App;

public sealed class RenderLoop
{
    private readonly DoubleBufferedConsole _console;
    private readonly FrameExchange<RenderFrame> _frames;
    private readonly ConsolePresentSystem _present;
    private readonly int _frameMs;
    private Func<RenderFrame> _frameFactory;

    public RenderLoop(DoubleBufferedConsole console, FrameExchange<RenderFrame> frames,
        ConsolePresentSystem present, int frameMs, Func<RenderFrame> frameFactory)
    {
        _console = console;
        _frames = frames;
        _present = present;
        _frameMs = frameMs;
        _frameFactory = frameFactory;
    }

    public void Run(CancellationToken token)
    {
        _console.AttachToCurrentThread();
        _frames.AttachReader();

        var (lastWidth, lastHeight) = _console.GetWindowSize();
        Console.Error.WriteLine($"[RenderLoop] Старт: {lastWidth}x{lastHeight}");

        while (!token.IsCancellationRequested)
        {
            var (currentWidth, currentHeight) = _console.GetWindowSize();

            if (currentWidth != lastWidth || currentHeight != lastHeight)
            {
                Console.Error.WriteLine($"[RenderLoop] Изменение: {lastWidth}x{lastHeight} -> {currentWidth}x{currentHeight}");
                Console.Error.WriteLine($"[RenderLoop] Буфер: {_console.Width}x{_console.Height}");

                lastWidth = currentWidth;
                lastHeight = currentHeight;

                int newWidth = currentWidth;
                int newHeight = currentHeight;
                _frameFactory = () => new RenderFrame(newWidth, newHeight);

                Console.Error.WriteLine($"[RenderLoop] Вызываем Resize({currentWidth}, {currentHeight})");
                _console.Resize(currentWidth, currentHeight);
                _frames.Resize(_frameFactory);

                Console.Error.WriteLine($"[RenderLoop] Resize завершён. Новый буфер: {_console.Width}x{_console.Height}");
            }

            _present.Present();
            Thread.Sleep(_frameMs);
        }
    }
}