// Data/TileInfoRegistry.cs
using System.Collections.Generic;

namespace AsciiFarm.Data;

public sealed class TileInfoRegistry
{
    private readonly List<ITileInfoProvider> _providers = new();

    public void Register(ITileInfoProvider provider) => _providers.Add(provider);

    public IEnumerable<string> GetAllInfo(int x, int y, char tile, Core.World world)
    {
        foreach (var provider in _providers)
        {
            foreach (var line in provider.GetInfo(x, y, tile, world))
            {
                yield return line;
            }
        }
    }
}