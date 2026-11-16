// App/GameApplication.cs
using AsciiFarm.Content;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Input;
using AsciiFarm.Rendering;
using AsciiFarm.Serialization;
using AsciiFarm.Systems;
using System.Collections.Concurrent;
using System.Reflection;

namespace AsciiFarm.App;

public sealed class GameApplication
{
    private readonly GameSettings _settings;
    public GameApplication(GameSettings settings) => _settings = settings;

    public void Run()
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("Нужна настоящая консоль: ввод перенаправлен.");

        try
        {
            Console.BufferWidth = _settings.ScreenWidth;
            Console.BufferHeight = _settings.ScreenHeight;
            Console.WindowWidth = _settings.ScreenWidth;
            Console.WindowHeight = _settings.ScreenHeight;
        }
        catch { /* остаёмся на текущем размере */ }

        int actualWidth = Console.WindowWidth;
        int actualHeight = Console.WindowHeight;

        var world = GameWorldBuilder.Build(_settings);
        var channel = new CommandChannel(_settings.CommandQueueCapacity);
        var frames = new FrameExchange<RenderFrame>(() => new RenderFrame(actualWidth, actualHeight));

        using var console = new DoubleBufferedConsole(actualWidth, actualHeight);
        using var shutdown = new CancellationTokenSource();
        var errors = new ConcurrentQueue<Exception>();

        KeyBindings bindings = _settings.UseJsonContent && Directory.Exists(_settings.ContentRoot)
            ? new ContentLoader(_settings.ContentRoot).LoadKeyBindings()
            : GameContent.CreateKeyBindings();

        var snapshot = new SnapshotSystem(frames, new MapRenderer(), new EntityRenderer(), new HudRenderer());

        var simulation = new SimulationLoop(
            world, channel, SystemPipeline.CreateSimulationSystems(),
            snapshot, frames, shutdown, _settings.SimulationFps);

        var fpsCounter = world.Resource<FpsCounter>();
        var render = new RenderLoop(console, frames,
            new ConsolePresentSystem(console, frames, fpsCounter),
            1000 / _settings.RenderFps);

        var input = new InputLoop(channel, bindings, _settings.InputPollMs);

        var threads = new[]
        {
            Start("Simulation", () => simulation.Run(shutdown.Token), shutdown, errors),
            Start("Render",     () => render.Run(shutdown.Token),     shutdown, errors),
            Start("Input",      () => input.Run(shutdown.Token),      shutdown, errors),
        };
        foreach (var thread in threads) thread.Join();

        channel.Close();
        if (!errors.IsEmpty) throw new AggregateException("Сбой игрового потока.", errors);

        world.Guard.Detach();
        if (world.Resource<GameState>().NeedsSave) SaveGame(world);
    }

    private void SaveGame(World world)
    {
        var registry = ComponentSerializerRegistry.AutoRegister(Assembly.GetExecutingAssembly());
        new ComponentSaveSystem(registry).Save(SafePath.Resolve(_settings.MapPath), world);
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