// Core/ComponentStore.cs
namespace AsciiFarm.Core;

public interface IComponentStore
{
    int Count { get; }
    IReadOnlyCollection<Entity> Entities { get; }
    bool Has(Entity entity);
    void Remove(Entity entity);
    // ДОБАВИТЬ ЭТО:
    IEnumerable<(Entity Entity, object Component)> AllComponentsBoxed();
}

/// <summary>Только хранилище данных. Никакой игровой логики.</summary>
public sealed class ComponentStore<T> : IComponentStore where T : struct
{
    private readonly Dictionary<Entity, T> _items = new();

    public int Count => _items.Count;
    public IReadOnlyCollection<Entity> Entities => _items.Keys;

    public bool Has(Entity entity) => _items.ContainsKey(entity);
    public void Set(Entity entity, T component) => _items[entity] = component;
    public T Get(Entity entity) => _items[entity];
    public bool TryGet(Entity entity, out T component) => _items.TryGetValue(entity, out component);
    public void Remove(Entity entity) => _items.Remove(entity);
    // Добавить в конец класса ComponentStore<T>

    /// <summary>
    /// Итератор по всем компонентам (упакованный в object для универсального доступа).
    /// </summary>
    public IEnumerable<(Entity Entity, object Component)> AllComponentsBoxed()
    {
        foreach (var kvp in _items)
            yield return (kvp.Key, kvp.Value);
    }
}