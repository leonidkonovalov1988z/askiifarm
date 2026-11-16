// Serialization/ComponentSerializers.cs
using System;
using System.IO;
using AsciiFarm.Components;
using AsciiFarm.Core;

namespace AsciiFarm.Serialization;

public sealed class PositionSerializer : ComponentSerializer<Position>
{
    public override string TypeName => nameof(Position);

    public override void Serialize(BinaryWriter w, Position p)
    {
        w.Write(p.X);
        w.Write(p.Y);
    }

    public override Position Deserialize(BinaryReader r, int version)
    {
        int x = r.ReadInt32();
        int y = r.ReadInt32();
        return new Position(x, y);
    }
}

public sealed class TileMapSerializer : ComponentSerializer<TileMapComponent>
{
    /// <summary>P0-5: лимит тайлов, чтобы битый файл не вызвал OOM при аллокации.</summary>
    public const int MaxTiles = 2_000_000;   // ~36 МБ при 18 байтах на тайл

    public override string TypeName => nameof(TileMapComponent);
    public override int Version => 2;

    public override void Serialize(BinaryWriter w, TileMapComponent m)
    {
        int width = m.Width, height = m.Height;
        w.Write(width);
        w.Write(height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var tile = m.Tiles[x, y];
                w.Write(tile.Symbol);
                w.Write(tile.Nitrogen);
                w.Write(tile.Phosphorus);
                w.Write(tile.Potassium);
                w.Write(tile.Moisture);
            }
    }

    public override TileMapComponent Deserialize(BinaryReader r, int version)
    {
        if (version is < 1 or > 2)
            throw new InvalidDataException($"TileMap: неизвестная версия {version}.");

        int width = r.ReadInt32();
        int height = r.ReadInt32();
        ValidateSize(width, height);          // проверка ДО new TileData[...]

        var tiles = new TileData[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                char symbol = r.ReadChar();
                tiles[x, y] = version == 1
                    ? MigrateTile(symbol)
                    : new TileData
                    {
                        Symbol = symbol,
                        Nitrogen = r.ReadSingle(),
                        Phosphorus = r.ReadSingle(),
                        Potassium = r.ReadSingle(),
                        Moisture = r.ReadSingle()
                    };
            }

        return new TileMapComponent { Tiles = tiles };
    }

    private static void ValidateSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new InvalidDataException($"TileMap: некорректный размер {width}x{height}.");

        if ((long)width * height > MaxTiles)
            throw new InvalidDataException(
                $"TileMap: {width}x{height} превышает лимит {MaxTiles} тайлов.");
    }

    private static TileData MigrateTile(char symbol) => symbol switch
    {
        '≈' => new TileData { Symbol = symbol, Nitrogen = 0f, Phosphorus = 0f, Potassium = 0f, Moisture = 1.0f },
        '♣' or '♠' => new TileData { Symbol = symbol, Nitrogen = 0.7f, Phosphorus = 0.5f, Potassium = 0.6f, Moisture = 0.7f },
        '.' or ',' or '\'' or '`' or ';' => new TileData { Symbol = symbol, Nitrogen = 0.5f, Phosphorus = 0.3f, Potassium = 0.4f, Moisture = 0.6f },
        '#' or '▒' or '┌' or '┐' or '└' or '┘' or '─' or '│' or '┬' or '┴' or '├' or '┤' or '┼' or '╪'
            or '╔' or '╗' or '╚' or '╝' or '═' or '║' or '╠' or '╣' or '╦' or '╩' or '╬'
            => new TileData { Symbol = symbol, Nitrogen = 0f, Phosphorus = 0f, Potassium = 0f, Moisture = 0f },
        ' ' => new TileData { Symbol = symbol, Nitrogen = 0.1f, Phosphorus = 0.1f, Potassium = 0.1f, Moisture = 0.1f },
        _ => new TileData { Symbol = symbol, Nitrogen = 0.3f, Phosphorus = 0.3f, Potassium = 0.3f, Moisture = 0.4f }
    };
}

public sealed class PlayerTagSerializer : ComponentSerializer<PlayerTag>
{
    public override string TypeName => nameof(PlayerTag);
    public override void Serialize(BinaryWriter w, PlayerTag p) { }
    public override PlayerTag Deserialize(BinaryReader r, int version) => new();
}

public sealed class GlyphSerializer : ComponentSerializer<Glyph>
{
    public override string TypeName => nameof(Glyph);

    public override void Serialize(BinaryWriter w, Glyph g)
    {
        w.Write(g.Symbol);
        w.Write((int)g.Foreground);
        w.Write((int)g.Background);
        w.Write(g.Layer);
    }

    public override Glyph Deserialize(BinaryReader r, int version) => new()
    {
        Symbol = r.ReadChar(),
        // Clamp: битый сейв не должен ронять рендер в MakeAttribute
        Foreground = (ConsoleColor)Math.Clamp(r.ReadInt32(), 0, 15),
        Background = (ConsoleColor)Math.Clamp(r.ReadInt32(), 0, 15),
        Layer = r.ReadInt32()
    };
}