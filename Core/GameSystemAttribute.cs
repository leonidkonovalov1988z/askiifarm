// Core/GameSystemAttribute.cs
using System;
namespace AsciiFarm.Core;

/// <summary>
/// Помечает класс как игровую систему. Order определяет порядок выполнения.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GameSystemAttribute : Attribute
{
    public int Order { get; set; }
}