using System;
namespace AsciiFarm.Core;

/// <summary>
/// Помечает компонент как временный — не сохраняется в файл.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class DontSaveAttribute : Attribute { }