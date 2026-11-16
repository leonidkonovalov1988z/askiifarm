// Core/IComponentSerializer.cs
using System;
using System.IO;

namespace AsciiFarm.Core;

public interface IComponentSerializer
{
    Type ComponentType { get; }
    string TypeName { get; }
    int Version { get; }
    void Serialize(BinaryWriter writer, object component);
    object Deserialize(BinaryReader reader, int version);

    /// <summary>P2-16: делегат применения к миру, строится один раз, без рефлексии.</summary>
    Action<World, Entity, object> Applier { get; }
}

public abstract class ComponentSerializer<T> : IComponentSerializer where T : struct
{
    private Action<World, Entity, object>? _applier;

    public Type ComponentType => typeof(T);
    public abstract string TypeName { get; }
    public virtual int Version => 1;

    public Action<World, Entity, object> Applier =>
        _applier ??= static (world, entity, component) => world.Add(entity, (T)component);

    public abstract void Serialize(BinaryWriter writer, T component);
    public abstract T Deserialize(BinaryReader reader, int version);

    void IComponentSerializer.Serialize(BinaryWriter writer, object component) => Serialize(writer, (T)component);
    object IComponentSerializer.Deserialize(BinaryReader reader, int version) => Deserialize(reader, version);
}