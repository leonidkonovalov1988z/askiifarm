// Serialization/ComponentSaveSystem.cs
using System.Reflection;
using System.Text;
using AsciiFarm.Core;

namespace AsciiFarm.Serialization;

/// <summary>
/// Формат: "AFARM2" | int version | int count | записи { string type, int ver, int id, int len, byte[len] }.
/// </summary>
public sealed class ComponentSaveSystem
{
    private static readonly byte[] Signature = Encoding.ASCII.GetBytes("AFARM2");
    public const int FormatVersion = 1;

    // P0-5: жёсткие лимиты против «злых»/битых файлов
    public const int MaxComponentBytes = 64 * 1024 * 1024;   // 64 МБ на компонент
    public const int MaxRecords = 1_000_000;
    private const int MinRecordBytes = 13;                   // 1 (string) + 4 + 4 + 4

    private readonly ComponentSerializerRegistry _registry;
    public ComponentSaveSystem(ComponentSerializerRegistry registry) => _registry = registry;

    public void Save(string filePath, World world)
    {
        var entries = new List<Entry>();
        var visitor = new SaveVisitor(_registry, entries);

        foreach (var (componentType, store) in world.AllStores())
        {
            if (componentType.GetCustomAttribute<DontSaveAttribute>() != null) continue;
            if (_registry.Get(componentType) == null) continue;
            store.Accept(visitor);      // P2-14: типобезопасный обход, без boxing
        }

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(Signature);
        writer.Write(FormatVersion);
        writer.Write(entries.Count);
        foreach (var e in entries)
        {
            writer.Write(e.TypeName);
            writer.Write(e.Version);
            writer.Write(e.EntityId);
            writer.Write(e.Data.Length);
            writer.Write(e.Data);
        }
    }

    public void Load(string filePath, World world)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        var sig = reader.ReadBytes(Signature.Length);
        if (!sig.AsSpan().SequenceEqual(Signature))
            throw new InvalidDataException("Неверная сигнатура файла сохранения.");

        int formatVersion = reader.ReadInt32();
        if (formatVersion <= 0 || formatVersion > FormatVersion)
            throw new InvalidDataException($"Формат {formatVersion} не поддерживается (максимум {FormatVersion}).");

        int count = reader.ReadInt32();
        if (count < 0 || count > MaxRecords || count > (stream.Length - stream.Position) / MinRecordBytes)
            throw new InvalidDataException($"Некорректное число записей: {count}.");

        int maxEntityId = -1;
        for (int i = 0; i < count; i++)
        {
            string typeName = reader.ReadString();
            int version = reader.ReadInt32();
            int entityId = reader.ReadInt32();
            int dataLen = reader.ReadInt32();

            if (entityId < 0)
                throw new InvalidDataException($"Некорректный ID сущности: {entityId}.");
            if (dataLen < 0 || dataLen > MaxComponentBytes)
                throw new InvalidDataException($"Компонент '{typeName}': недопустимый размер {dataLen} (лимит {MaxComponentBytes}).");
            if (dataLen > stream.Length - stream.Position)
                throw new InvalidDataException($"Компонент '{typeName}': файл оборван.");

            byte[] data = reader.ReadBytes(dataLen);
            if (data.Length != dataLen) throw new EndOfStreamException();

            var serializer = _registry.Get(typeName);
            if (serializer == null || version <= 0 || version > serializer.Version)
                continue;   // forward compatibility

            using var ms = new MemoryStream(data, writable: false);
            using var compReader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            var component = serializer.Deserialize(compReader, version);

            serializer.Applier(world, new Entity(entityId), component);   // P2-16: без рефлексии
            if (entityId > maxEntityId) maxEntityId = entityId;
        }

        if (maxEntityId >= 0) world.ReserveId(maxEntityId);
    }

    public static bool IsNewFormat(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        using var stream = File.OpenRead(filePath);
        if (stream.Length < Signature.Length) return false;
        var sig = new byte[Signature.Length];
        return stream.Read(sig, 0, sig.Length) == sig.Length && sig.AsSpan().SequenceEqual(Signature);
    }

    private readonly record struct Entry(string TypeName, int Version, int EntityId, byte[] Data);

    /// <summary>Обход компонентов без упаковки: T известен статически.</summary>
    private sealed class SaveVisitor : IComponentVisitor
    {
        private readonly ComponentSerializerRegistry _registry;
        private readonly List<Entry> _entries;
        private readonly MemoryStream _ms = new();
        private readonly BinaryWriter _writer;

        public SaveVisitor(ComponentSerializerRegistry registry, List<Entry> entries)
        {
            _registry = registry;
            _entries = entries;
            _writer = new BinaryWriter(_ms, Encoding.UTF8, leaveOpen: true);
        }

        public void Visit<T>(Entity entity, in T component) where T : struct
        {
            var typed = _registry.Get<T>();
            if (typed != null)
            {
                Reset();
                typed.Serialize(_writer, component);
                _writer.Flush();
                _entries.Add(new Entry(typed.TypeName, typed.Version, entity.Id, _ms.ToArray()));
                return;
            }

            // Fallback для сериализаторов, не наследующих ComponentSerializer<T>.
            var untyped = _registry.Get(typeof(T));
            if (untyped == null) return;
            Reset();
            untyped.Serialize(_writer, component);
            _writer.Flush();
            _entries.Add(new Entry(untyped.TypeName, untyped.Version, entity.Id, _ms.ToArray()));
        }

        private void Reset() { _ms.SetLength(0); _ms.Position = 0; }
    }
}