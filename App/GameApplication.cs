using AsciiFarm.Components;
using AsciiFarm.Content;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Input;
using AsciiFarm.Rendering;
using AsciiFarm.Serialization;
using AsciiFarm.Systems;
using askiifarm;
using askiifarm.Rendering;
using askiifarm.Serialization;
using System.Collections.Concurrent;
namespace AsciiFarm.App;

public sealed class GameApplication
{
    private readonly GameSettings _settings;

    public GameApplication(GameSettings settings) => _settings = settings;

    // App/GameApplication.cs — только метод Run(), остальное без изменений
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

        // Загружаем привязки из того же источника, что и остальной контент
        KeyBindings bindings;
        if (_settings.UseJsonContent && Directory.Exists(_settings.ContentRoot))
        {
            bindings = new ContentLoader(_settings.ContentRoot).LoadKeyBindings();
        }
        else
        {
            bindings = GameContent.CreateKeyBindings();
        }

        var simulation = new SimulationLoop(world, channel, SystemPipeline.CreateSimulationSystems(),
            new SnapshotSystem(frames), frames, shutdown);
        var render = new RenderLoop(console, frames,
            new ConsolePresentSystem(console, frames), 1000 / _settings.RenderFps);
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
        var state = world.Resource<GameState>();
        if (state.NeedsSave) SaveGame(world);
    }
    // Заменить метод SaveGame на:

    private void SaveGame(World world)
    {
        var registry = ComponentSerializerRegistry.AutoRegister(
            System.Reflection.Assembly.GetExecutingAssembly());
        var saveSystem = new ComponentSaveSystem(registry);
        saveSystem.Save(_settings.MapPath, world);
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