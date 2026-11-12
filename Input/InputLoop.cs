// Input/InputLoop.cs
using AsciiFarm.Data;
namespace AsciiFarm.Input;

/// <summary>Тело потока ввода. Ссылки на World нет по построению.</summary>
public sealed class InputLoop
{
    private readonly CommandChannel _channel;
    private readonly KeyBindings _bindings;
    private readonly int _pollDelayMs;

    public InputLoop(CommandChannel channel, KeyBindings bindings, int pollDelayMs)
    {
        _channel = channel;
        _bindings = bindings;
        _pollDelayMs = pollDelayMs;
    }

    public void Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            bool worked = false;
            while (Console.KeyAvailable)                       // не блокируемся → выходим по токену
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (_bindings.Map.TryGetValue(key, out var command))
                {
                    _channel.Post(command);
                    worked = true;
                }
            }
            if (!worked) Thread.Sleep(_pollDelayMs);
        }
    }
}