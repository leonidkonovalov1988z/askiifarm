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

/// <summary>Амбар: зерно/семена по культурам, посадочный материал, овощи, волокно.</summary>
public struct GranaryComponent
{
    public Dictionary<string, float> Items;
}

// РОВНО ОДНО объявление, на уровне пространства имён (не внутри Granary!)
public enum FarmAction : byte { Plant = 0, Harvest = 1, CycleCrop = 2, PlantYear2 = 3 }

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