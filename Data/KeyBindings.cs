// Data/KeyBindings.cs
using AsciiFarm.Input;

namespace AsciiFarm.Data;

/// <summary>Заполняется при старте, дальше только читается потоком ввода.</summary>
public sealed class KeyBindings
{
    public Dictionary<ConsoleKey, GameCommand> Map { get; } = new();
}