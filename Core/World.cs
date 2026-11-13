// Core/World.cs
namespace AsciiFarm.Core;

/// <summary>
/// Данные мира. Принадлежит РОВНО ОДНОМУ потоку (симуляции).
/// Render- и Input-потоки ссылки на World не получают вообще.
/// </summary>
public sealed class World
{
    private readonly Dictionary<Type, IComponentStore> _stores = new();
    private readonly Dictionary<Type, object> _resources = new();
    private int _nextEntityId;

    public ThreadGuard Guard { get; } = new("World");

    public Entity CreateEntity()
    {
        Guard.Validate();
        return new Entity(_nextEntityId++);
    }

    public ComponentStore<T> Store<T>() where T : struct
    {
        Guard.Validate();
        if (!_stores.TryGetValue(typeof(T), out var store))
        {
            store = new ComponentStore<T>();
            _stores[typeof(T)] = store;
        }
        return (ComponentStore<T>)store;
    }

    public void Add<T>(Entity entity, T component) where T : struct => Store<T>().Set(entity, component);

    // Запросы материализуются в массив: системы могут спокойно удалять компоненты в цикле.
    public Entity[] Query<TA>() where TA : struct => Store<TA>().Entities.ToArray();

    public Entity[] Query<TA, TB>() where TA : struct where TB : struct
    {
        IComponentStore a = Store<TA>(), b = Store<TB>();
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        return small.Entities.Where(large.Has).ToArray();
    }

    public Entity[] Query<TA, TB, TC>() where TA : struct where TB : struct where TC : struct
    {
        IComponentStore c = Store<TC>();
        return Query<TA, TB>().Where(c.Has).ToArray();
    }

    public T Resource<T>() where T : class
    {
        Guard.Validate();
        return (T)_resources[typeof(T)];
    }

    public void SetResource<T>(T resource) where T : class
    {
        Guard.Validate();
        _resources[typeof(T)] = resource;
    }
    // Добавить в конец класса World, перед закрывающей скобкой

    /// <summary>
    /// Итератор по всем хранилищам компонентов.
    /// </summary>
    public IEnumerable<(Type ComponentType, IComponentStore Store)> AllStores()
    {
        Guard.Validate();
        foreach (var kvp in _stores)
            yield return (kvp.Key, kvp.Value);
    }

    /// <summary>
    /// Резервирует ID сущности, чтобы новые сущности не получили уже использованный ID.
    /// </summary>
    public void ReserveId(int id)
    {
        Guard.Validate();
        if (id >= _nextEntityId)
            _nextEntityId = id + 1;
    }
}