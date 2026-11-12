// Content/ContentLoader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AsciiFarm.Components;
using AsciiFarm.Data;
using AsciiFarm.Input;
using askiifarm;

namespace AsciiFarm.Content;

/// <summary>
/// Загружает контент из JSON-файлов. Правка баланса/цветов/клавиш — без перекомпиляции.
/// </summary>
public sealed class ContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    // DTO для палитры
    private sealed class PaletteDto
    {
        public TileStyleDto Fallback { get; set; } = new();
        public Dictionary<string, TileStyleDto> Tiles { get; set; } = new();
    }
    private sealed class TileStyleDto
    {
        public ConsoleColor Foreground { get; set; } = ConsoleColor.Gray;
        public ConsoleColor Background { get; set; } = ConsoleColor.Black;
        public bool BlocksMovement { get; set; }
    }

    // DTO для привязок
    private sealed class KeyBindingsDto
    {
        public Dictionary<string, string> Bindings { get; set; } = new();
    }

    // DTO для персонажей
    private sealed class CharactersDto
    {
        public string DefaultPlayerKey { get; set; } = "player";
        public Dictionary<string, GlyphDto> Characters { get; set; } = new();
    }
    private sealed class GlyphDto
    {
        public string Symbol { get; set; } = "?";
        public ConsoleColor Foreground { get; set; } = ConsoleColor.White;
        public ConsoleColor Background { get; set; } = ConsoleColor.Black;
        public int Layer { get; set; }
    }

    public string ContentRoot { get; }

    public ContentLoader(string contentRoot)
    {
        ContentRoot = contentRoot;
    }

    public TilePalette LoadPalette(string fileName = "palette.json")
    {
        var path = Path.Combine(ContentRoot, fileName);
        var dto = JsonSerializer.Deserialize<PaletteDto>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException($"Не удалось прочитать {path}");

        var palette = new TilePalette
        {
            Fallback = ToStyle(dto.Fallback)
        };
        foreach (var kvp in dto.Tiles)
        {
            if (kvp.Key.Length == 1)
                palette.Tiles[kvp.Key[0]] = ToStyle(kvp.Value);
        }
        return palette;
    }

    public KeyBindings LoadKeyBindings(string fileName = "keybindings.json")
    {
        var path = Path.Combine(ContentRoot, fileName);
        var dto = JsonSerializer.Deserialize<KeyBindingsDto>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException($"Не удалось прочитать {path}");

        var bindings = new KeyBindings();
        foreach (var kvp in dto.Bindings)
        {
            if (Enum.TryParse<ConsoleKey>(kvp.Key, true, out var key) &&
                Enum.TryParse<GameCommand>(kvp.Value, true, out var cmd))
            {
                bindings.Map[key] = cmd;
            }
        }
        return bindings;
    }

    public CharacterPalette LoadCharacterPalette(string fileName = "characters.json")
    {
        var path = Path.Combine(ContentRoot, fileName);
        var dto = JsonSerializer.Deserialize<CharactersDto>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException($"Не удалось прочитать {path}");

        var palette = new CharacterPalette { DefaultPlayerKey = dto.DefaultPlayerKey };
        foreach (var kvp in dto.Characters)
        {
            palette.Characters[kvp.Key] = new Glyph
            {
                Symbol = kvp.Value.Symbol.Length > 0 ? kvp.Value.Symbol[0] : '?',
                Foreground = kvp.Value.Foreground,
                Background = kvp.Value.Background,
                Layer = kvp.Value.Layer
            };
        }
        return palette;
    }

    private static TileStyle ToStyle(TileStyleDto dto) =>
        new(dto.Foreground, dto.Background, dto.BlocksMovement);
}