// Data/CropVisuals.cs
using AsciiFarm.Components;

namespace AsciiFarm.Data;

public static class CropVisuals
{
    public const int Layer = 5;   // над картой (0), под игроком (10)

    public static Glyph For(CropDefinition crop, PlantStage stage)
    {
        var look = stage switch
        {
            PlantStage.Germination => (',', ConsoleColor.DarkGreen),
            PlantStage.Vegetative => ('"', ConsoleColor.Green),
            PlantStage.Flowering => (Cp437Symbols.BlackClubSuit, ConsoleColor.Yellow),
            PlantStage.Ripe => Ripe(crop),
            _ => ('x', ConsoleColor.DarkGray)
        };

        return new Glyph
        {
            Symbol = look.Item1,
            Foreground = look.Item2,
            Background = ConsoleColor.Black,
            Layer = Layer
        };
    }

    private static (char, ConsoleColor) Ripe(CropDefinition c) => c.Mode switch
    {
        HarvestMode.Biennial => (Cp437Symbols.InverseBullet, ConsoleColor.White),
        HarvestMode.EarlyOrLate => (Cp437Symbols.BlackUpPointingTriangle, ConsoleColor.Cyan),
        _ => (Cp437Symbols.WhiteSunWithRays, ConsoleColor.DarkYellow)
    };
}