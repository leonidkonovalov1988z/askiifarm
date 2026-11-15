using System.Collections.Generic;

namespace AsciiFarm.Data;

/// <summary>Реестр провайдеров информации о тайле.</summary>
public sealed class TileInfoRegistry
{
    private readonly List<ITileInfoProvider> _providers = new();

    public void Register(ITileInfoProvider provider) => _providers.Add(provider);

    public IEnumerable<string> GetAllInfo(char tile, Core.World world)
    {
        foreach (var provider in _providers)
        {
            foreach (var line in provider.GetInfo(tile, world))
            {
                yield return line;
            }
        }
    }
}