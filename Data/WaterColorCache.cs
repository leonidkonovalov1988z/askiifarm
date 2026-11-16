// Data/WaterColorCache.cs
namespace AsciiFarm.Data;

/// <summary>Переиспользуемый кэш цветов воды. Живёт в мире, не пересоздаётся каждый тик.</summary>
public sealed class WaterColorCache
{
    public Dictionary<(int X, int Y), TileStyle> Colors { get; } = new();
    public int LastBucket { get; set; } = int.MinValue;
}