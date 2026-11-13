using System;
using System.IO;
namespace AsciiFarm.Core;

/// <summary>
/// Интерфейс сериализации компонента. Каждый компонент имеет свой сериализатор.
/// </summary>
public interface IComponentSerializer
{
    /// <summary>Тип компонента (например, typeof(Position)).</summary>
    Type ComponentType { get; }

    /// <summary>Имя типа для хранения в файле.</summary>
    string TypeName { get; }

    /// <summary>Текущая версия формата сериализации.</summary>
    int Version { get; }

    void Serialize(BinaryWriter writer, object component);
    object Deserialize(BinaryReader reader, int version);
}

/// <summary>
/// Базовый класс для типобезопасных сериализаторов.
/// </summary>
public abstract class ComponentSerializer<T> : IComponentSerializer where T : struct
{
    public Type ComponentType => typeof(T);
    public abstract string TypeName { get; }
    public virtual int Version => 1;

    public abstract void Serialize(BinaryWriter writer, T component);
    public abstract T Deserialize(BinaryReader reader, int version);

    void IComponentSerializer.Serialize(BinaryWriter writer, object component) =>
        Serialize(writer, (T)component);

    object IComponentSerializer.Deserialize(BinaryReader reader, int version) =>
        Deserialize(reader, version);
}