// Input/InputLoop.cs
using AsciiFarm.Data;
namespace AsciiFarm.Input;

/// <summary>
/// Тело потока ввода. Ссылки на World нет по построению.
/// </summary>
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
                var keyInfo = Console.ReadKey(intercept: true);
                var key = keyInfo.Key;

                // Модификаторы: запоминаем последний
                if (key == ConsoleKey.X) { _lastModifier = Modifier.Speed; worked = true; continue; }
                if (key == ConsoleKey.P) { _lastModifier = Modifier.FastForward; worked = true; continue; }

                // Цифры 1-4 с модификатором
                if (key >= ConsoleKey.D1 && key <= ConsoleKey.D5)
                {
                    int n = key - ConsoleKey.D1 + 1;
                    if (_lastModifier == Modifier.Speed)
                    {
                        var cmd = n switch
                        {
                            1 => GameCommand.Speed1,
                            2 => GameCommand.Speed2,
                            3 => GameCommand.Speed3,
                            4 => GameCommand.Speed4,
                            5 => GameCommand.Speed5,
                            _ => GameCommand.None
                        };
                        _channel.Post(cmd);
                        _lastModifier = Modifier.None;
                        worked = true;
                        continue;
                    }
                    if (_lastModifier == Modifier.FastForward)
                    {
                        var cmd = n switch
                        {
                            1 => GameCommand.FastForward1,
                            2 => GameCommand.FastForward2,
                            3 => GameCommand.FastForward3,
                            4 => GameCommand.FastForward4,
                            5 => GameCommand.FastForward5,
                            _ => GameCommand.None
                        };
                        _channel.Post(cmd);
                        _lastModifier = Modifier.None;
                        worked = true;
                        continue;
                    }
                }

                // Сброс модификатора при любой другой клавише (кроме X/P)
                if (_lastModifier != Modifier.None && key != ConsoleKey.X && key != ConsoleKey.P)
                    _lastModifier = Modifier.None;

                // Обычные команды из биндингов
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