// Rendering/EntityRenderer.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Rendering;

public sealed class EntityRenderer : IFrameRenderer
{
    public void Render(RenderFrame frame, World world)
    {
        var characterPalette = world.Resource<CharacterPalette>();
        var glyphs = world.Store<Glyph>();
        var positions = world.Store<Position>();
        var playerTags = world.Store<PlayerTag>();

        foreach (var entity in world.Query<Glyph, Position>().OrderBy(e => glyphs.Get(e).Layer))
        {
            var glyph = glyphs.Get(entity);
            var pos = positions.Get(entity);
            if (playerTags.Has(entity))
                glyph = characterPalette.Get(characterPalette.DefaultPlayerKey);
            FrameDraw.Put(frame, pos.X, pos.Y, glyph.Symbol, glyph.Foreground, glyph.Background);
        }
    }
}