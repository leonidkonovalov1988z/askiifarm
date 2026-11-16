// Data/CropDefinition.cs
namespace AsciiFarm.Data;

public enum HarvestMode { GrainAndSeed, Biennial, EarlyOrLate }

/// <summary>Справочные (реальные) параметры культуры. Масштабирования здесь нет.</summary>
public sealed class CropDefinition
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsWinter { get; init; }

    public float TBase { get; init; }
    public float TOptMin { get; init; }
    public float TOptMax { get; init; }
    public float TCritColdGround { get; init; }
    public float TCritFrostFlower { get; init; }
    public float TCritHeat { get; init; }

    public double GddVeg { get; init; }
    public double GddFlower { get; init; }
    public double GddEarlyHarvest { get; init; }
    public double GddHarvest { get; init; }
    public double GddSeedYear2 { get; init; }

    public float SoilMoistMin { get; init; }
    public float SoilMoistOptMin { get; init; }
    public float SoilMoistOptMax { get; init; }
    public float SoilMoistMax { get; init; }
    public float SoilMoistRipeningMax { get; init; }
    public float AirHumDiseaseMin { get; init; }

    public double SolarMjTarget { get; init; }
    public HarvestMode Mode { get; init; }
    public float SeedRatio { get; init; }

    /// <summary>
    /// Вынос N/P/K за ПОЛНЫЙ сезон (за solar_mj_target биомассы).
    /// Отрицательный — истощение, положительный — обогащение (бобовые).
    /// Размазывается по росту пропорционально приросту биомассы.
    /// </summary>
    public float SoilDelta { get; init; }

    public double TargetGdd(bool isYear2) =>
        isYear2 && GddSeedYear2 > 0 ? GddSeedYear2 : GddHarvest;

    public float RipeningMoistureMax =>
        SoilMoistRipeningMax > 0 ? SoilMoistRipeningMax : SoilMoistMax;
}

public sealed class CropCatalog
{
    private readonly Dictionary<string, CropDefinition> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CropDefinition> _list = new();

    public IReadOnlyList<CropDefinition> All => _list;
    public int Count => _list.Count;
    public string? LoadError { get; set; }

    public void Add(CropDefinition crop)
    {
        if (_byId.ContainsKey(crop.Id)) return;
        _byId[crop.Id] = crop;
        _list.Add(crop);
    }

    public bool TryGet(string? id, out CropDefinition crop)
    {
        if (id != null && _byId.TryGetValue(id, out var found)) { crop = found; return true; }
        crop = null!;
        return false;
    }

    public CropDefinition? ByIndex(int index) =>
        _list.Count == 0 ? null : _list[Math.Clamp(index, 0, _list.Count - 1)];
}