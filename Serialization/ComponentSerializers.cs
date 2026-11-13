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

    public override Position Deserialize(BinaryReader r, int version) =>
        new(r.ReadInt32(), r.ReadInt32());
}

public sealed class TileMapSerializer : ComponentSerializer<TileMapComponent>
{
    public override string TypeName => nameof(TileMapComponent);

    public override void Serialize(BinaryWriter w, TileMapComponent m)
    {
        int width = m.Width, height = m.Height;
        w.Write(width);
        w.Write(height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                w.Write(m.Tiles[x, y]);
    }

    public override TileMapComponent Deserialize(BinaryReader r, int version)
    {
        int width = r.ReadInt32(), height = r.ReadInt32();
        char[,] tiles = new char[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tiles[x, y] = r.ReadChar();
        return new TileMapComponent { Tiles = tiles };
    }
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
        Foreground = (ConsoleColor)r.ReadInt32(),
        Background = (ConsoleColor)r.ReadInt32(),
        Layer = r.ReadInt32()
    };
}