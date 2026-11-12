using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace askiifarm.Serialization
{
    public class GameStorage
    {
        private static readonly byte[] BinarySignature = Encoding.ASCII.GetBytes("AFARM");
        private readonly Dictionary<Version, IVersionFormatHandler> _handlers = new();
        private readonly Dictionary<Version, IVersionConverter> _converters = new();

        public static Version LatestVersion => new Version(0, 0, 3);

        public GameStorage()
        {
            RegisterHandler(new FormatHandlerV0_0_0());
            RegisterHandler(new FormatHandlerV0_0_1());
            RegisterHandler(new FormatHandlerV0_0_2());
            RegisterHandler(new FormatHandlerV0_0_3());

            RegisterConverter(new ConverterV0_0_0ToV0_0_1());
            RegisterConverter(new ConverterV0_0_1ToV0_0_2());
            RegisterConverter(new ConverterV0_0_2ToV0_0_3());
            RegisterConverter(new ConverterV0_0_3ToV0_0_2());
        }

        public void RegisterHandler(IVersionFormatHandler handler) =>
            _handlers[handler.Version] = handler;

        public void RegisterConverter(IVersionConverter converter) =>
            _converters[converter.SourceVersion] = converter;

        public void Save(string filePath, Version version, object data)
        {
            if (!_handlers.TryGetValue(version, out var handler))
                throw new NotSupportedException($"Обработчик для версии {version} не найден.");
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            if (version > new Version(0, 0, 0))
            {
                using var headerWriter = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
                headerWriter.Write(BinarySignature);
                headerWriter.Write(version.Major);
                headerWriter.Write(version.Minor);
                headerWriter.Write(version.Build);
            }
            handler.Save(stream, data);
        }

        public (Version Version, object Data) Load(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            Version detectedVersion;
            if (HasBinarySignature(stream))
            {
                using var headerReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                int major = headerReader.ReadInt32();
                int minor = headerReader.ReadInt32();
                int build = headerReader.ReadInt32();
                detectedVersion = new Version(major, minor, build);
            }
            else
            {
                stream.Position = 0;
                detectedVersion = new Version(0, 0, 0);
            }
            if (!_handlers.TryGetValue(detectedVersion, out var handler))
                throw new NotSupportedException($"Неизвестная версия сохранения: {detectedVersion}");
            object data = handler.Load(stream);
            return (detectedVersion, data);
        }

        public object ConvertStep(Version currentVersion, object data)
        {
            if (!_converters.TryGetValue(currentVersion, out var converter))
                throw new InvalidOperationException($"Нет конвертера для перехода с версии {currentVersion}.");
            return converter.Convert(data);
        }

        public T LoadAndConvert<T>(string filePath, Version targetVersion) where T : class
        {
            var (currentVersion, currentData) = Load(filePath);
            while (currentVersion < targetVersion)
            {
                if (!_converters.TryGetValue(currentVersion, out var converter))
                    throw new InvalidOperationException($"Не найден конвертер для версии {currentVersion}.");
                currentData = converter.Convert(currentData);
                currentVersion = converter.TargetVersion;
            }
            if (currentVersion != targetVersion)
                throw new InvalidOperationException($"Версия файла ({currentVersion}) выше целевой ({targetVersion}).");
            return currentData as T
                ?? throw new InvalidCastException($"Данные версии {targetVersion} не соответствуют типу {typeof(T).Name}.");
        }

        public T LoadLatest<T>(string filePath) where T : class
        {
            return LoadAndConvert<T>(filePath, LatestVersion);
        }

        /// <summary>
        /// Сохраняет данные. Если версия файла совпадает — перезаписывает.
        /// Если файл старее — конвертирует прочитанное и перезаписывает тем же путём.
        /// Если файл новее — создаёт новый файл с суффиксом версии.
        /// </summary>
        public void SaveSmart(string filePath, Version version, object data)
        {
            if (!File.Exists(filePath))
            {
                Save(filePath, version, data);
                return;
            }

            var (existingVersion, _) = Load(filePath);

            if (existingVersion == version)
            {
                // Та же версия — просто перезаписываем
                Save(filePath, version, data);
            }
            else if (existingVersion < version)
            {
                // Файл старее: перезаписываем его новой версией
                Save(filePath, version, data);
            }
            else
            {
                // Файл новее — создаём отдельный файл с суффиксом
                string dir = Path.GetDirectoryName(filePath) ?? ".";
                string name = Path.GetFileNameWithoutExtension(filePath);
                string ext = Path.GetExtension(filePath);
                string newFilePath = Path.Combine(dir, $"{name}_v{version.Major}.{version.Minor}.{version.Build}{ext}");
                Save(newFilePath, version, data);
            }
        }

        private static bool HasBinarySignature(Stream stream)
        {
            if (stream.Length < BinarySignature.Length)
                return false;
            byte[] buffer = new byte[BinarySignature.Length];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return bytesRead == BinarySignature.Length && buffer.SequenceEqual(BinarySignature);
        }
    }
}