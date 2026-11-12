using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;   // ← добавить

namespace askiifarm.Serialization
{
    // --- Модели данных ---

    /// <summary>
    /// Версия 0.0.0: Карта, загруженная из сырого текстового файла.
    /// </summary>
    /// 

    public class GameDataV0_0_0
    {
        public char[,] Map { get; set; } = new char[0, 0];
    }

    /// <summary>
    /// Версия 0.0.1: Та же карта, но уже в стандарте бинарного сохранения.
    /// </summary>
    public class GameDataV0_0_1
    {
        public char[,] Map { get; set; } = new char[0, 0];
    }

    // --- Интерфейсы ---

    /// <summary>
    /// Общий интерфейс для чтения/записи конкретной версии.
    /// </summary>
    public interface IVersionFormatHandler
    {
        Version Version { get; }
        void Save(Stream stream, object data);
        object Load(Stream stream);
    }

    /// <summary>
    /// Интерфейс конвертера из версии X-1 в версию X.
    /// </summary>
    public interface IVersionConverter
    {
        Version SourceVersion { get; }
        Version TargetVersion { get; }
        object Convert(object previousVersionData);
    }

    // --- Обработчик версии 0.0.0 (Текстовый файл с картой) ---
    public class FormatHandlerV0_0_0 : IVersionFormatHandler
    {
        public Version Version => new Version(0, 0, 0);

        public void Save(Stream stream, object data)
        {
            if (data is not GameDataV0_0_0 state)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_0)}");

            // Сохраняем в обычный текст (UTF-8), если понадобится выгрузить обратно
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            int width = state.Map.GetLength(0);
            int height = state.Map.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                char[] row = new char[width];
                for (int x = 0; x < width; x++)
                {
                    row[x] = state.Map[x, y];
                }
                writer.WriteLine(row);
            }
        }

        public object Load(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            string content = reader.ReadToEnd();

            // Разбиваем на строки, убирая символы переноса \r и \n
            string[] lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            // Убираем пустые строки в самом конце файла (если при копировании остался лишний Enter)
            int height = lines.Length;
            while (height > 0 && string.IsNullOrEmpty(lines[height - 1]))
            {
                height--;
            }

            if (height == 0)
                return new GameDataV0_0_0 { Map = new char[0, 0] };

            // Находим максимальную ширину строки
            int width = 0;
            for (int y = 0; y < height; y++)
            {
                if (lines[y].Length > width)
                    width = lines[y].Length;
            }

            // Заполняем двумерный массив [x, y]
            char[,] map = new char[width, height];
            for (int y = 0; y < height; y++)
            {
                string line = lines[y];
                for (int x = 0; x < width; x++)
                {
                    // Если строка короче максимальной ширины, добиваем пробелом
                    map[x, y] = x < line.Length ? line[x] : ' ';
                }
            }

            return new GameDataV0_0_0 { Map = map };
        }
    }

    // --- Обработчик версии 0.0.1 (Бинарный формат с заголовком версии) ---
    public class FormatHandlerV0_0_1 : IVersionFormatHandler
    {
        public Version Version => new Version(0, 0, 1);

        public void Save(Stream stream, object data)
        {
            if (data is not GameDataV0_0_1 state)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_1)}");

            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            int width = state.Map.GetLength(0);
            int height = state.Map.GetLength(1);

            writer.Write(width);
            writer.Write(height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    writer.Write(state.Map[x, y]);
                }
            }
        }

        public object Load(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();

            char[,] map = new char[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map[x, y] = reader.ReadChar();
                }
            }

            return new GameDataV0_0_1 { Map = map };
        }
    }

    // --- Конвертер из версии 0.0.0 в версию 0.0.1 ---
    public class ConverterV0_0_0ToV0_0_1 : IVersionConverter
    {
        public Version SourceVersion => new Version(0, 0, 0);
        public Version TargetVersion => new Version(0, 0, 1);

        public object Convert(object previousVersionData)
        {
            if (previousVersionData is not GameDataV0_0_0 v0)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_0)}");

            // Клонируем массив карты, чтобы данные были независимыми
            char[,] clonedMap = (char[,])v0.Map.Clone();

            return new GameDataV0_0_1
            {
                Map = clonedMap
            };
        }
    }
}