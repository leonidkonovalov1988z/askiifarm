using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace AsciiFarm.Core;

/// <summary>
/// Реестр сериализаторов компонентов. Поддерживает авто-регистрацию через рефлексию.
/// </summary>
public sealed class ComponentSerializerRegistry
{
    private readonly Dictionary<Type, IComponentSerializer> _byType = new();
    private readonly Dictionary<string, IComponentSerializer> _byName = new();

    public void Register(IComponentSerializer serializer)
    {
        _byType[serializer.ComponentType] = serializer;
        _byName[serializer.TypeName] = serializer;
    }

    public IComponentSerializer? Get(Type componentType) =>
        _byType.TryGetValue(componentType, out var s) ? s : null;

    public IComponentSerializer? Get(string typeName) =>
        _byName.TryGetValue(typeName, out var s) ? s : null;

    public IEnumerable<IComponentSerializer> All => _byType.Values;

    /// <summary>
    /// Авто-регистрация: ищет все классы, наследующие IComponentSerializer, в сборке.
    /// </summary>
    public static ComponentSerializerRegistry AutoRegister(Assembly assembly)
    {
        var registry = new ComponentSerializerRegistry();
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsAbstract && typeof(IComponentSerializer).IsAssignableFrom(type))
            {
                var instance = (IComponentSerializer)Activator.CreateInstance(type)!;
                registry.Register(instance);
            }
        }
        return registry;
    }
}