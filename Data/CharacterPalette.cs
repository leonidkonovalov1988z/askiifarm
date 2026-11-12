// Data/CharacterPalette.cs
using System.Collections.Generic;
using AsciiFarm.Components;
namespace AsciiFarm.Data;

/// <summary>Палитра символов персонажей. Чистые данные.</summary>
public sealed class CharacterPalette
{
    public Dictionary<string, Glyph> Characters { get; } = new();
    public string DefaultPlayerKey { get; set; } = "player";

    public Glyph Get(string key) =>
        Characters.TryGetValue(key, out var glyph)
            ? glyph
            : throw new KeyNotFoundException($"Символ '{key}' не найден");
}