// Serialization/PlantSerializers.cs
using System;
using System.IO;
using AsciiFarm.Components;
using AsciiFarm.Core;

namespace AsciiFarm.Serialization;

public sealed class PlantComponentSerializer : ComponentSerializer<PlantComponent>
{
    public override string TypeName => nameof(PlantComponent);

    public override void Serialize(BinaryWriter w, PlantComponent p)
    {
        w.Write(p.CropId ?? "");
        w.Write(p.GddAccumulated);
        w.Write(p.BiomassMj);
        w.Write(p.Health);
        w.Write(p.Disease);
        w.Write(p.IsYear2);
        w.Write((byte)p.Stage);
    }

    public override PlantComponent Deserialize(BinaryReader r, int version)
    {
        string id = r.ReadString();
        if (id.Length > 128) throw new InvalidDataException("Plant: слишком длинный crop_id.");

        double gdd = r.ReadDouble();
        double biomass = r.ReadDouble();
        float health = r.ReadSingle();
        float disease = r.ReadSingle();
        bool year2 = r.ReadBoolean();
        byte stage = r.ReadByte();

        return new PlantComponent
        {
            CropId = id,
            GddAccumulated = Math.Clamp(gdd, 0.0, 1e9),
            BiomassMj = Math.Clamp(biomass, 0.0, 1e9),
            Health = Math.Clamp(health, 0f, 1f),
            Disease = Math.Clamp(disease, 0f, 1e6f),
            IsYear2 = year2,
            Stage = (PlantStage)Math.Min(stage, (byte)PlantStage.Dead)
        };
    }
}

public sealed class GranarySerializer : ComponentSerializer<GranaryComponent>
{
    public const int MaxItems = 10_000;
    public override string TypeName => nameof(GranaryComponent);

    public override void Serialize(BinaryWriter w, GranaryComponent g)
    {
        var items = g.Items ?? new Dictionary<string, float>();
        w.Write(items.Count);
        foreach (var kv in items) { w.Write(kv.Key); w.Write(kv.Value); }
    }

    public override GranaryComponent Deserialize(BinaryReader r, int version)
    {
        int count = r.ReadInt32();
        if (count < 0 || count > MaxItems)
            throw new InvalidDataException($"Granary: недопустимое число позиций {count}.");

        var items = new Dictionary<string, float>(count, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < count; i++)
        {
            string key = r.ReadString();
            float value = r.ReadSingle();
            if (key.Length > 128) throw new InvalidDataException("Granary: слишком длинный ключ.");
            if (!float.IsFinite(value) || value < 0f) value = 0f;
            items[key] = Math.Min(value, 1e9f);
        }
        return new GranaryComponent { Items = items };
    }
}