// Core/SafePath.cs
namespace AsciiFarm.Core;

/// <summary>
/// Песочница путей: ни один файл не читается/пишется вне рабочей директории.
/// Корень по умолчанию — текущая директория (как и было у относительных путей).
/// Можно переопределить: SafePath.Root = AppContext.BaseDirectory.
/// </summary>
public static class SafePath
{
    private static readonly StringComparison Cmp =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static string _root = Path.GetFullPath(Directory.GetCurrentDirectory());

    public static string Root
    {
        get => _root;
        set => _root = Path.GetFullPath(value);
    }

    public static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Путь пуст.", nameof(path));
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new ArgumentException($"Недопустимые символы в пути: '{path}'.", nameof(path));

        string full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(_root, path));
        string prefix = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;

        if (!full.StartsWith(prefix, Cmp) && !string.Equals(full, _root, Cmp))
            throw new UnauthorizedAccessException(
                $"Путь '{path}' выходит за пределы рабочей директории '{_root}'.");

        return full;
    }
}