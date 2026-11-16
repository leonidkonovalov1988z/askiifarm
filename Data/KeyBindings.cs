// Data/KeyBindings.cs
using AsciiFarm.Input;

namespace AsciiFarm.Data;

public sealed class KeyBindings
{
    public Dictionary<ConsoleKey, GameCommand> Map { get; } = new();

    // P1-9: модификаторы — данные, а не хардкод
    public ConsoleKey SpeedModifier { get; set; } = ConsoleKey.X;
    public ConsoleKey FastForwardModifier { get; set; } = ConsoleKey.P;
}