// App/GameApplication.cs
using AsciiFarm.Content;
using AsciiFarm.Core;
using AsciiFarm.Input;
using AsciiFarm.Rendering;
using AsciiFarm.Systems;
using askiifarm;
using askiifarm.Rendering;
using System.Collections.Concurrent;
namespace AsciiFarm.App;

public sealed class GameApplication
{
    private readonly GameSettings _settings;

    public GameApplication(GameSettings settings) => _settings = settings;

    public void Run()
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("Нужна настоящая консоль: ввод перенаправлен.");

        var world = GameWorldBuilder.Build(_settings);
        var channel = new CommandChannel(_settings.CommandQueueCapacity);
        var frames = new FrameExchange<RenderFrame>(
            () => new RenderFrame(_settings.ScreenWidth, _settings.ScreenHeight));

        using var console = new DoubleBufferedConsole(_settings.ScreenWidth, _settings.ScreenHeight);
        using var shutdown = new CancellationTokenSource();
        var errors = new ConcurrentQueue<Exception>();

        var simulation = new SimulationLoop(world, channel, SystemPipeline.CreateSimulationSystems(),
            new SnapshotSystem(frames), frames, 1000 / _settings.SimulationFps, shutdown);
        var render = new RenderLoop(console, frames,
            new ConsolePresentSystem(console, frames), 1000 / _settings.RenderFps);
        var input = new InputLoop(channel, GameContent.CreateKeyBindings(), _settings.InputPollMs);

        var threads = new[]
        {
            Start("Simulation", () => simulation.Run(shutdown.Token), shutdown, errors),
            Start("Render",     () => render.Run(shutdown.Token),     shutdown, errors),
            Start("Input",      () => input.Run(shutdown.Token),      shutdown, errors),
        };

        foreach (var thread in threads) thread.Join();   // консоль освобождаем только после остановки рендера
        channel.Close();

        if (!errors.IsEmpty) throw new AggregateException("Сбой игрового потока.", errors);
    }

    private static Thread Start(string name, Action body,
                                CancellationTokenSource shutdown, ConcurrentQueue<Exception> errors)
    {
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { errors.Enqueue(ex); shutdown.Cancel(); }
        })
        { Name = name, IsBackground = true };
        thread.Start();
        return thread;
    }
}