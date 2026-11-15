// Data/ITileInfoProvider.cs
using System.Collections.Generic;

namespace AsciiFarm.Data;

public interface ITileInfoProvider
{
    /// <summary>Возвращает строки информации для тайла по конкретным координатам.</summary>
    IEnumerable<string> GetInfo(int x, int y, char tile, Core.World world);
}