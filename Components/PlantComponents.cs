// Components/PlantComponents.cs
using AsciiFarm.Core;

namespace AsciiFarm.Components;

public enum PlantStage : byte { Germination = 0, Vegetative = 1, Flowering = 2, Ripe = 3, Dead = 4 }

/// <summary>Состояние растения. Накопления уже масштабированы под игровой год.</summary>
public struct PlantComponent
{
    public string CropId;
    public double GddAccumulated;   // GDD-часы
    public double BiomassMj;
    public float Health;            // 0..1
    public float Disease;           // 0..∞
    public bool IsYear2;
    public PlantStage Stage;
}

/// <summary>Плодородие почвы по тайлам (0.2..1.0). Компонент на сущности карты.</summary>
public struct SoilFertilityMap
{
    public float[,] Values;
    public int Width => Values.GetLength(0);
    public int Height => Values.GetLength(1);
}

/// <summary>Амбар: зерно/семена по культурам, посадочный материал, овощи, волокно.</summary>
public struct GranaryComponent
{
    public Dictionary<string, float> Items;
}

public enum FarmAction : byte { Plant, Harvest, CycleCrop }

[DontSave]
public struct FarmIntent
{
    public FarmAction Action;
    public FarmIntent(FarmAction action) => Action = action;
}

/// <summary>Ключи и доступ к амбару. Словарь создаётся один раз, дальше мутируется на месте.</summary>
public static class Granary
{
    public const string Vegetables = "#vegetables";
    public const string Fiber = "#fiber";
    public static string Root(string cropId) => cropId + "#root";

    public static float Get(GranaryComponent g, string key) =>
        g.Items != null && g.Items.TryGetValue(key, out var v) ? v : 0f;

    public static void Add(GranaryComponent g, string key, float amount)
    {
        if (g.Items == null || amount <= 0f) return;
        g.Items[key] = Get(g, key) + amount;
    }

    public static void Take(GranaryComponent g, string key, float amount)
    {
        if (g.Items == null) return;
        g.Items[key] = MathF.Max(0f, Get(g, key) - amount);
    }
}