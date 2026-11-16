// Core/ComponentStore.cs
namespace AsciiFarm.Core;

/// <summary>Типобезопасный обход компонентов: T известен статически, boxing нет.</summary>
public interface IComponentVisitor
{
    void Visit<T>(Entity entity, in T component) where T : struct;
}

public interface IComponentStore
{
    int Count { get; }
    int StructureVersion { get; }          // меняется только при добавлении/удалении сущности
    IReadOnlyCollection<Entity> Entities { get; }
    bool Has(Entity entity);
    void Remove(Entity entity);
    void Accept(IComponentVisitor visitor);
}

/// <summary>Только хранилище данных. Никакой игровой логики.</summary>
public sealed class ComponentStore<T> : IComponentStore where T : struct
{
    private readonly Dictionary<Entity, T> _items = new();

    public int Count => _items.Count;
    public int StructureVersion { get; private set; }
    public IReadOnlyCollection<Entity> Entities => _items.Keys;

    public bool Has(Entity entity) => _items.ContainsKey(entity);

    public void Set(Entity entity, T component)
    {
        if (!_items.ContainsKey(entity)) StructureVersion++;   // перезапись значения версию не меняет
        _items[entity] = component;
    }

    public T Get(Entity entity) => _items[entity];
    public bool TryGet(Entity entity, out T component) => _items.TryGetValue(entity, out component);

    public void Remove(Entity entity)
    {
        if (_items.Remove(entity)) StructureVersion++;
    }

    public void Accept(IComponentVisitor visitor)
    {
        foreach (var kvp in _items) visitor.Visit(kvp.Key, kvp.Value);
    }

    /// <summary>Альтернатива для простых случаев: делегат типизирован, компонент не упаковывается.</summary>
    public void Visit(Action<Entity, T> action)
    {
        foreach (var kvp in _items) action(kvp.Key, kvp.Value);
    }
}