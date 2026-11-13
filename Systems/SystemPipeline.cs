// Systems/SystemPipeline.cs
using System;
using System.Linq;
using System.Reflection;
using AsciiFarm.Core;
namespace AsciiFarm.Systems;

public static class SystemPipeline
{
    public static ISystem[] CreateSimulationSystems()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(ISystem).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => new { Type = t, Attr = t.GetCustomAttribute<GameSystemAttribute>() })
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr.Order)
            .Select(x => (ISystem)(Activator.CreateInstance(x.Type) ?? throw new InvalidOperationException($"Не удалось создать экземпляр {x.Type.Name}")))
            .ToArray();
    }
}