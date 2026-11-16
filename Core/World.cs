// Core/World.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace AsciiFarm.Core;

/// <summary>
/// Данные мира. Принадлежат РОВНО ОДНОМУ потоку (симуляции).
/// ВНИМАНИЕ: массивы из Query&lt;...&gt; кэшируются — вызывающий код не должен их изменять.
/// Нужна мутабельная копия — делайте .ToArray() самостоятельно.
/// </summary>
public sealed class World
{
    private readonly Dictionary<Type, IComponentStore> _stores = new();
    private readonly Dictionary<Type, object> _resources = new();
    private readonly Dictionary<QueryKey, CachedQuery> _queryCache = new();
    private int _nextEntityId;
    private int _schemaVersion;   // растёт при появлении нового типа компонента

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
            _schemaVersion++;
            _queryCache.Clear();
        }
        return (ComponentStore<T>)store;
    }

    public void Add<T>(Entity entity, T component) where T : struct => Store<T>().Set(entity, component);

    public Entity[] Query<TA>() where TA : struct
    {
        IComponentStore a = Store<TA>();
        var key = new QueryKey(typeof(TA), null, null);
        if (TryGetCached(key, a, null, null, out var hit)) return hit;

        var result = a.Entities.ToArray();
        PutCache(key, a, null, null, result);
        return result;
    }

    public Entity[] Query<TA, TB>() where TA : struct where TB : struct
    {
        IComponentStore a = Store<TA>();
        IComponentStore b = Store<TB>();
        var key = new QueryKey(typeof(TA), typeof(TB), null);
        if (TryGetCached(key, a, b, null, out var hit)) return hit;

        IComponentStore small = a.Count <= b.Count ? a : b;
        IComponentStore large = a.Count <= b.Count ? b : a;

        var result = small.Entities.Where(large.Has).ToArray();
        PutCache(key, a, b, null, result);
        return result;
    }

    public Entity[] Query<TA, TB, TC>() where TA : struct where TB : struct where TC : struct
    {
        IComponentStore a = Store<TA>();
        IComponentStore b = Store<TB>();
        IComponentStore c = Store<TC>();
        var key = new QueryKey(typeof(TA), typeof(TB), typeof(TC));
        if (TryGetCached(key, a, b, c, out var hit)) return hit;

        IComponentStore small = a.Count <= b.Count ? a : b;
        IComponentStore large = a.Count <= b.Count ? b : a;

        var result = small.Entities.Where(e => large.Has(e) && c.Has(e)).ToArray();
        PutCache(key, a, b, c, result);
        return result;
    }

    public T Resource<T>() where T : class
    {
        Guard.Validate();
        return (T)_resources[typeof(T)];
    }

    /// <summary>Отсутствие ресурса — не исключение.</summary>
    public bool TryGetResource<T>(out T? resource) where T : class
    {
        Guard.Validate();
        if (_resources.TryGetValue(typeof(T), out var obj) && obj is T typed)
        {
            resource = typed;
            return true;
        }
        resource = null;
        return false;
    }

    public void SetResource<T>(T resource) where T : class
    {
        Guard.Validate();
        _resources[typeof(T)] = resource;
    }

    /// <summary>Итератор по всем хранилищам компонентов.</summary>
    public IEnumerable<(Type ComponentType, IComponentStore Store)> AllStores()
    {
        Guard.Validate();
        foreach (var kvp in _stores) yield return (kvp.Key, kvp.Value);
    }

    /// <summary>Резервирует ID, чтобы новые сущности не конфликтовали с загруженными.</summary>
    public void ReserveId(int id)
    {
        Guard.Validate();
        if (id >= _nextEntityId) _nextEntityId = id + 1;
    }

    // --- кэш запросов: инвалидация по структурным версиям хранилищ ---
    private bool TryGetCached(QueryKey key, IComponentStore a, IComponentStore? b, IComponentStore? c,
                              out Entity[] result)
    {
        if (_queryCache.TryGetValue(key, out var cached)
            && cached.Schema == _schemaVersion
            && cached.Va == a.StructureVersion
            && cached.Vb == (b?.StructureVersion ?? 0)
            && cached.Vc == (c?.StructureVersion ?? 0))
        {
            result = cached.Entities;
            return true;
        }
        result = Array.Empty<Entity>();
        return false;
    }

    private void PutCache(QueryKey key, IComponentStore a, IComponentStore? b, IComponentStore? c, Entity[] result) =>
        _queryCache[key] = new CachedQuery(_schemaVersion, a.StructureVersion,
                                           b?.StructureVersion ?? 0, c?.StructureVersion ?? 0, result);

    private readonly record struct QueryKey(Type A, Type? B, Type? C);
    private readonly record struct CachedQuery(int Schema, int Va, int Vb, int Vc, Entity[] Entities);
}