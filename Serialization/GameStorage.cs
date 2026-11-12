using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace askiifarm.Serialization
{
    public class GameStorage
    {
        // Магическая сигнатура для бинарных файлов (начиная с версии 0.0.1)
        private static readonly byte[] BinarySignature = Encoding.ASCII.GetBytes("AFARM");

        private readonly Dictionary<Version, IVersionFormatHandler> _handlers = new();
        private readonly Dictionary<Version, IVersionConverter> _converters = new();

        public GameStorage()
        {
            RegisterHandler(new FormatHandlerV0_0_0());
            RegisterHandler(new FormatHandlerV0_0_1());

            RegisterConverter(new ConverterV0_0_0ToV0_0_1());
        }

        public void RegisterHandler(IVersionFormatHandler handler) =>
            _handlers[handler.Version] = handler;

        public void RegisterConverter(IVersionConverter converter) =>
            _converters[converter.SourceVersion] = converter;

        /// <summary>
        /// Сохраняет данные указанной версии на диск.
        /// </summary>
        public void Save(string filePath, Version version, object data)
        {
            if (!_handlers.TryGetValue(version, out var handler))
                throw new NotSupportedException($"Обработчик для версии {version} не найден.");

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

            // Если версия >= 0.0.1, пишем бинарный заголовок и номер версии
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

        /// <summary>
        /// Читает файл, автоматически определяет его версию и распаковывает в соответствующий класс.
        /// </summary>
        public (Version Version, object Data) Load(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            Version detectedVersion;

            // Проверяем, начинается ли файл с бинарной сигнатуры "AFARM"
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
                // Если сигнатуры нет — считаем, что это сырой текстовый файл версии 0.0.0
                stream.Position = 0;
                detectedVersion = new Version(0, 0, 0);
            }

            if (!_handlers.TryGetValue(detectedVersion, out var handler))
                throw new NotSupportedException($"Неизвестная версия сохранения: {detectedVersion}");

            object data = handler.Load(stream);
            return (detectedVersion, data);
        }

        /// <summary>
        /// Конвертирует данные из версии X-1 в версию X.
        /// </summary>
        public object ConvertStep(Version currentVersion, object data)
        {
            if (!_converters.TryGetValue(currentVersion, out var converter))
                throw new InvalidOperationException($"Нет конвертера для перехода с версии {currentVersion}.");

            return converter.Convert(data);
        }

        /// <summary>
        /// Загружает файл любой старой версии и прогоняет через цепочку конвертеров до целевой версии.
        /// </summary>
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
