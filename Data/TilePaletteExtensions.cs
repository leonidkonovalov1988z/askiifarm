// Data/TilePaletteExtensions.cs
namespace AsciiFarm.Data;

/// <summary>Логика рядом с данными, но не внутри них.</summary>
public static class TilePaletteExtensions
{
    public static TileStyle Resolve(this TilePalette palette, char tile) =>
        palette.Tiles.TryGetValue(tile, out var style) ? style : palette.Fallback;
}