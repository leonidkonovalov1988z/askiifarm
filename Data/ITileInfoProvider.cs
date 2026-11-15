using System.Collections.Generic;

namespace AsciiFarm.Data;

/// <summary>
/// Провайдер информации о тайле для UI.
/// Реализации могут собирать данные из разных источников.
/// </summary>
public interface ITileInfoProvider
{
    /// <summary>Возвращает строки информации для данного тайла.</summary>
    IEnumerable<string> GetInfo(char tile, Core.World world);
}