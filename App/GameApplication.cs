using AsciiFarm.Components;
using AsciiFarm.Content;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Input;
using AsciiFarm.Rendering;
using AsciiFarm.Serialization;
using AsciiFarm.Systems;
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

        // 1. Сначала пытаемся установить размер окна консоли
        try
        {
            Console.BufferWidth = _settings.ScreenWidth;
            Console.BufferHeight = _settings.ScreenHeight;
            Console.WindowWidth = _settings.ScreenWidth;
            Console.WindowHeight = _settings.ScreenHeight;
        }
        catch
        {
            // Если не удалось — используем текущий размер
        }

        // 2. Получаем реальный размер окна (может отличаться от запрошенного)
        int actualWidth = Console.WindowWidth;
        int actualHeight = Console.WindowHeight;

        // 3. Создаём мир и канал команд
        var world = GameWorldBuilder.Build(_settings);
        var channel = new CommandChannel(_settings.CommandQueueCapacity);

        // 4. Factory для создания кадров нужного размера
        Func<RenderFrame> frameFactory = () => new RenderFrame(actualWidth, actualHeight);
        var frames = new FrameExchange<RenderFrame>(frameFactory);

        using var console = new DoubleBufferedConsole(actualWidth, actualHeight);
        using var shutdown = new CancellationTokenSource();
        var errors = new ConcurrentQueue<Exception>();

        // 5. Загружаем привязки
        KeyBindings bindings;
        if (_settings.UseJsonContent && Directory.Exists(_settings.ContentRoot))
        {
            bindings = new ContentLoader(_settings.ContentRoot).LoadKeyBindings();
        }
        else
        {
            bindings = GameContent.CreateKeyBindings();
        }

        // 6. Создаём симуляцию
        var simulation = new SimulationLoop(
            world, channel, SystemPipeline.CreateSimulationSystems(),
            new SnapshotSystem(frames), frames, shutdown, _settings.SimulationFps);

        // 7. Создаём рендер с передачей factory
        var fpsCounter = world.Resource<FpsCounter>();
        var render = new RenderLoop(
            console, frames,
            new ConsolePresentSystem(console, frames, fpsCounter),
            1000 / _settings.RenderFps,
            frameFactory);

        // 8. Создаём ввод
        var input = new InputLoop(channel, bindings, _settings.InputPollMs);

        // 9. Запускаем потоки
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