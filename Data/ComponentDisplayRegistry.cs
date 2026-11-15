// Data/ComponentDisplayRegistry.cs
using System;
using System.Collections.Generic;

namespace AsciiFarm.Data;

/// <summary>
/// Реестр правил форматирования компонентов для отображения в UI.
/// Позволяет добавлять поддержку новых компонентов без изменения кода отрисовки.
/// </summary>
public static class ComponentDisplayRegistry
{
    private static readonly Dictionary<Type, Func<object, string>> Formatters = new();

    /// <summary>
    /// Зарегистрировать правило отображения для компонента.
    /// Пример: ComponentDisplayRegistry.Register<PlantComponent>(p => $"{p.Species} (Рост: {p.Growth:P0})");
    /// </summary>
    public static void Register<T>(Func<T, string> formatter)
    {
        Formatters[typeof(T)] = obj => formatter((T)obj);
    }

    /// <summary>
    /// Получить строковое представление компонента. Если правило не задано, вызывает ToString().
    /// </summary>
    public static string Format(object component)
    {
        if (component == null) return "null";

        if (Formatters.TryGetValue(component.GetType(), out var formatter))
            return formatter(component);

        return component.ToString(); // Запасной вариант
    }
}