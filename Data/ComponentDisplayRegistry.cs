// Data/ComponentDisplayRegistry.cs
namespace AsciiFarm.Data;

public static class ComponentDisplayRegistry
{
    private static readonly Dictionary<Type, object> Formatters = new();   // Func<T, string>

    public static void Register<T>(Func<T, string> formatter) => Formatters[typeof(T)] = formatter;

    /// <summary>Типобезопасный формат: без упаковки структуры в object.</summary>
    public static string Format<T>(in T component) =>
        Formatters.TryGetValue(typeof(T), out var f)
            ? ((Func<T, string>)f)(component)
            : component?.ToString() ?? "null";
}