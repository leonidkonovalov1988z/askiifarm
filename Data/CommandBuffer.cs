// Data/CommandBuffer.cs
using AsciiFarm.Input;
namespace AsciiFarm.Data;

/// <summary>Локальный (не разделяемый) буфер команд на один тик.</summary>
public sealed class CommandBuffer
{
    public List<GameCommand> Items { get; } = new();
}