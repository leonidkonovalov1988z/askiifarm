// Input/InputLoop.cs
using AsciiFarm.Data;

namespace AsciiFarm.Input;

public sealed class InputLoop
{
    private enum Modifier { None, Speed, FastForward }

    private readonly CommandChannel _channel;
    private readonly KeyBindings _bindings;
    private readonly int _pollDelayMs;
    private Modifier _lastModifier = Modifier.None;

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
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;

                if (key == _bindings.SpeedModifier)
                { _lastModifier = Modifier.Speed; worked = true; continue; }

                if (key == _bindings.FastForwardModifier)
                { _lastModifier = Modifier.FastForward; worked = true; continue; }

                if (key >= ConsoleKey.D1 && key <= ConsoleKey.D5 && _lastModifier != Modifier.None)
                {
                    int n = key - ConsoleKey.D1 + 1;
                    _channel.Post(_lastModifier == Modifier.Speed ? SpeedCommand(n) : ForwardCommand(n));
                    _lastModifier = Modifier.None;
                    worked = true;
                    continue;
                }

                _lastModifier = Modifier.None;   // любая другая клавиша сбрасывает модификатор

                if (_bindings.Map.TryGetValue(key, out var command))
                { _channel.Post(command); worked = true; }
            }
            if (!worked) Thread.Sleep(_pollDelayMs);
        }
    }

    private static GameCommand SpeedCommand(int n) => n switch
    {
        1 => GameCommand.Speed1,
        2 => GameCommand.Speed2,
        3 => GameCommand.Speed3,
        4 => GameCommand.Speed4,
        5 => GameCommand.Speed5,
        _ => GameCommand.None
    };

    private static GameCommand ForwardCommand(int n) => n switch
    {
        1 => GameCommand.FastForward1,
        2 => GameCommand.FastForward2,
        3 => GameCommand.FastForward3,
        4 => GameCommand.FastForward4,
        5 => GameCommand.FastForward5,
        _ => GameCommand.None
    };
}