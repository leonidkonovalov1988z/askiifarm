// Content/GameContent.cs
using AsciiFarm.Components;
using AsciiFarm.Data;
using AsciiFarm.Input;
using askiifarm;
namespace AsciiFarm.Content;

public static class GameContent
{
    public static TilePalette CreatePalette()
    {
        var p = new TilePalette { Fallback = new TileStyle(ConsoleColor.Gray, ConsoleColor.Black, false) };

        void Set(string chars, ConsoleColor fg, ConsoleColor bg = ConsoleColor.Black, bool blocks = false)
        {
            foreach (char c in chars) p.Tiles[c] = new TileStyle(fg, bg, blocks);
        }

        Set(".,'`;", ConsoleColor.Green);
        Set("\"", ConsoleColor.Yellow);
        Set("♣♠", ConsoleColor.DarkGreen, blocks: true);
        Set("≈", ConsoleColor.Cyan); // базовый, переопределяется RiverFlowSystem
        Set("┌┐└┘─│┬┴├┤▒#", ConsoleColor.DarkYellow, blocks: true);
        Set("┼╪", ConsoleColor.DarkYellow);
        Set("Θ", ConsoleColor.Red, blocks: true);
        Set("≡", ConsoleColor.White, ConsoleColor.DarkBlue, blocks: true);
        Set("╔╗╚╝═║╠╣╦╩╬", ConsoleColor.Gray, blocks: true);

        return p;
    }

    public static KeyBindings CreateKeyBindings()
    {
        var b = new KeyBindings();
        void Bind(GameCommand command, params ConsoleKey[] keys)
        {
            foreach (var k in keys) b.Map[k] = command;
        }

        Bind(GameCommand.MoveUp, ConsoleKey.W, ConsoleKey.UpArrow);
        Bind(GameCommand.MoveDown, ConsoleKey.S, ConsoleKey.DownArrow);
        Bind(GameCommand.MoveLeft, ConsoleKey.A, ConsoleKey.LeftArrow);
        Bind(GameCommand.MoveRight, ConsoleKey.D, ConsoleKey.RightArrow);
        Bind(GameCommand.Exit, ConsoleKey.Escape);
        return b;
    }

    public static CharacterPalette CreateCharacterPalette()
    {
        var palette = new CharacterPalette();

        palette.Characters["player"] = new Glyph
        {
            Symbol = Cp437Symbols.WhiteSmilingFace,
            Foreground = ConsoleColor.Yellow,
            Background = ConsoleColor.Black,
            Layer = 10
        };

        palette.Characters["warrior"] = new Glyph
        {
            Symbol = Cp437Symbols.BlackSpadeSuit,
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.Black,
            Layer = 10
        };

        palette.Characters["mage"] = new Glyph
        {
            Symbol = Cp437Symbols.EighthNote,
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
            Layer = 10
        };

        return palette;
    }

    public static Glyph PlayerGlyph => new()
    {
        Symbol = Cp437Symbols.WhiteSmilingFace,
        Foreground = ConsoleColor.Yellow,
        Background = ConsoleColor.Black,
        Layer = 10
    };
}