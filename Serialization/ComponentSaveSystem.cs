using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AsciiFarm.Core;
namespace AsciiFarm.Serialization;

/// <summary>
/// Компонентная система сохранения. Формат файла:
/// [6 байт: сигнатура "AFARM2"]
/// [4 байта: версия формата]
/// [4 байта: количество записей]
/// Для каждой записи:
///   [строка: имя типа компонента]
///   [4 байта: версия компонента]
///   [4 байта: ID сущности]
///   [4 байта: длина данных]
///   [N байт: данные]
/// 
/// Forward compatibility: неизвестные компоненты пропускаются.
/// Backward compatibility: старые компоненты читаются через миграции.
/// </summary>
public sealed class ComponentSaveSystem
{
    private static readonly byte[] Signature = Encoding.ASCII.GetBytes("AFARM2");
    public const int FormatVersion = 1;

    private readonly ComponentSerializerRegistry _registry;

    public ComponentSaveSystem(ComponentSerializerRegistry registry) => _registry = registry;

    public void Save(string filePath, World world)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        writer.Write(Signature);
        writer.Write(FormatVersion);

        var entries = new List<(string typeName, int version, int entityId, byte[] data)>();

        foreach (var (componentType, store) in world.AllStores())
        {
            // Пропускаем компоненты с [DontSave]
            if (componentType.GetCustomAttribute<DontSaveAttribute>() != null)
                continue;

            var serializer = _registry.Get(componentType);
            if (serializer == null) continue; // нет сериализатора — не сохраняем

            foreach (var (entity, component) in store.AllComponentsBoxed())
            {
                using var ms = new MemoryStream();
                using var compWriter = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
                serializer.Serialize(compWriter, component);
                entries.Add((serializer.TypeName, serializer.Version, entity.Id, ms.ToArray()));
            }
        }

        writer.Write(entries.Count);
        foreach (var (typeName, version, entityId, data) in entries)
        {
            writer.Write(typeName);
            writer.Write(version);
            writer.Write(entityId);
            writer.Write(data.Length);
            writer.Write(data);
        }
    }

    public void Load(string filePath, World world)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        var sig = reader.ReadBytes(6);
        if (!sig.AsSpan().SequenceEqual(Signature))
            throw new InvalidOperationException($"Неверная сигнатура файла сохранения");

        int formatVersion = reader.ReadInt32();
        if (formatVersion > FormatVersion)
            throw new InvalidOperationException($"Формат {formatVersion} новее поддерживаемого {FormatVersion}");

        int count = reader.ReadInt32();
        int maxEntityId = 0;

        for (int i = 0; i < count; i++)
        {
            string typeName = reader.ReadString();
            int version = reader.ReadInt32();
            int entityId = reader.ReadInt32();
            int dataLen = reader.ReadInt32();
            byte[] data = reader.ReadBytes(dataLen);

            var serializer = _registry.Get(typeName);
            if (serializer == null)
            {
                // Неизвестный компонент — пропускаем (forward compatibility!)
                continue;
            }

            if (version > serializer.Version)
            {
                // Версия компонента новее — пропускаем (forward compatibility!)
                continue;
            }

            using var ms = new MemoryStream(data);
            using var compReader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            var component = serializer.Deserialize(compReader, version);

            var entity = new Entity(entityId);

            // Добавляем компонент через рефлексию
            var addMethod = typeof(World).GetMethod(nameof(World.Add))!.MakeGenericMethod(serializer.ComponentType);
            addMethod.Invoke(world, new[] { entity, component });

            if (entityId > maxEntityId) maxEntityId = entityId;
        }

        // Резервируем ID, чтобы новые сущности не конфликтовали
        world.ReserveId(maxEntityId);
    }

    public static bool IsNewFormat(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        using var stream = File.OpenRead(filePath);
        if (stream.Length < 6) return false;
        var sig = new byte[6];
        stream.Read(sig, 0, 6);
        return sig.AsSpan().SequenceEqual(Signature);
    }
}