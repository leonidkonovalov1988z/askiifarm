using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace askiifarm.Serialization
{
    // ═══════════════════════════════════════════════════════════════
    //  МОДЕЛИ ДАННЫХ
    // ═══════════════════════════════════════════════════════════════

    public class GameDataV0_0_0
    {
        public char[,] Map { get; set; } = new char[0, 0];
    }

    public class GameDataV0_0_1
    {
        public char[,] Map { get; set; } = new char[0, 0];
    }

    public class GameDataV0_0_2
    {
        public char[,] Map { get; set; } = new char[0, 0];
        public int PlayerX { get; set; }
        public int PlayerY { get; set; }
    }

    public class GameDataV0_0_3
    {
        public char[,] Map { get; set; } = new char[0, 0];
        public int PlayerX { get; set; }
        public int PlayerY { get; set; }
        public Dictionary<char, TileColorData> TileColors { get; set; } = new();
    }

    public class TileColorData
    {
        public int Foreground { get; set; }
        public int Background { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  ИНТЕРФЕЙСЫ
    // ═══════════════════════════════════════════════════════════════

    public interface IVersionFormatHandler
    {
        Version Version { get; }
        void Save(Stream stream, object data);
        object Load(Stream stream);
    }

    public interface IVersionConverter
    {
        Version SourceVersion { get; }
        Version TargetVersion { get; }
        object Convert(object previousVersionData);
    }

    // ═══════════════════════════════════════════════════════════════
    //  ОБРАБОТЧИКИ ВЕРСИЙ
    // ═══════════════════════════════════════════════════════════════

    public class FormatHandlerV0_0_0 : IVersionFormatHandler
    {
        public Version Version => new Version(0, 0, 0);

        public void Save(Stream stream, object data)
        {
            if (data is not GameDataV0_0_0 state)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_0)}");
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            int width = state.Map.GetLength(0);
            int height = state.Map.GetLength(1);
            for (int y = 0; y < height; y++)
            {
                char[] row = new char[width];
                for (int x = 0; x < width; x++) row[x] = state.Map[x, y];
                writer.WriteLine(row);
            }
        }

        public object Load(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            string content = reader.ReadToEnd();
            string[] lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            int height = lines.Length;
            while (height > 0 && string.IsNullOrEmpty(lines[height - 1])) height--;
            if (height == 0) return new GameDataV0_0_0 { Map = new char[0, 0] };
            int width = 0;
            for (int y = 0; y < height; y++)
                if (lines[y].Length > width) width = lines[y].Length;
            char[,] map = new char[width, height];
            for (int y = 0; y < height; y++)
            {
                string line = lines[y];
                for (int x = 0; x < width; x++)
                    map[x, y] = x < line.Length ? line[x] : ' ';
            }
            return new GameDataV0_0_0 { Map = map };
        }
    }

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
                for (int x = 0; x < width; x++)
                    writer.Write(state.Map[x, y]);
        }

        public object Load(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            char[,] map = new char[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map[x, y] = reader.ReadChar();
            return new GameDataV0_0_1 { Map = map };
        }
    }

    public class FormatHandlerV0_0_2 : IVersionFormatHandler
    {
        public Version Version => new Version(0, 0, 2);

        public void Save(Stream stream, object data)
        {
            if (data is not GameDataV0_0_2 state)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_2)}");
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            int width = state.Map.GetLength(0);
            int height = state.Map.GetLength(1);
            writer.Write(width);
            writer.Write(height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    writer.Write(state.Map[x, y]);
            writer.Write(state.PlayerX);
            writer.Write(state.PlayerY);
        }

        public object Load(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            char[,] map = new char[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map[x, y] = reader.ReadChar();
            int playerX = reader.ReadInt32();
            int playerY = reader.ReadInt32();
            return new GameDataV0_0_2 { Map = map, PlayerX = playerX, PlayerY = playerY };
        }
    }

    public class FormatHandlerV0_0_3 : IVersionFormatHandler
    {
        public Version Version => new Version(0, 0, 3);

        public void Save(Stream stream, object data)
        {
            if (data is not GameDataV0_0_3 state)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_3)}");
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            int width = state.Map.GetLength(0);
            int height = state.Map.GetLength(1);
            writer.Write(width);
            writer.Write(height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    writer.Write(state.Map[x, y]);
            writer.Write(state.PlayerX);
            writer.Write(state.PlayerY);
            writer.Write(state.TileColors.Count);
            foreach (var kvp in state.TileColors)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Foreground);
                writer.Write(kvp.Value.Background);
            }
        }

        public object Load(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            char[,] map = new char[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map[x, y] = reader.ReadChar();
            int playerX = reader.ReadInt32();
            int playerY = reader.ReadInt32();
            int colorCount = reader.ReadInt32();
            var tileColors = new Dictionary<char, TileColorData>();
            for (int i = 0; i < colorCount; i++)
            {
                char key = reader.ReadChar();
                int fg = reader.ReadInt32();
                int bg = reader.ReadInt32();
                tileColors[key] = new TileColorData { Foreground = fg, Background = bg };
            }
            return new GameDataV0_0_3
            {
                Map = map,
                PlayerX = playerX,
                PlayerY = playerY,
                TileColors = tileColors
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  КОНВЕРТЕРЫ
    // ═══════════════════════════════════════════════════════════════

    public class ConverterV0_0_0ToV0_0_1 : IVersionConverter
    {
        public Version SourceVersion => new Version(0, 0, 0);
        public Version TargetVersion => new Version(0, 0, 1);
        public object Convert(object previousVersionData)
        {
            if (previousVersionData is not GameDataV0_0_0 v0)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_0)}");
            return new GameDataV0_0_1 { Map = (char[,])v0.Map.Clone() };
        }
    }

    public class ConverterV0_0_1ToV0_0_2 : IVersionConverter
    {
        public Version SourceVersion => new Version(0, 0, 1);
        public Version TargetVersion => new Version(0, 0, 2);
        public object Convert(object previousVersionData)
        {
            if (previousVersionData is not GameDataV0_0_1 v1)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_1)}");
            return new GameDataV0_0_2
            {
                Map = (char[,])v1.Map.Clone(),
                PlayerX = 28,
                PlayerY = 12
            };
        }
    }

    public class ConverterV0_0_2ToV0_0_3 : IVersionConverter
    {
        public Version SourceVersion => new Version(0, 0, 2);
        public Version TargetVersion => new Version(0, 0, 3);
        public object Convert(object previousVersionData)
        {
            // ВАЖНО: здесь должен быть GameDataV0_0_2, а не GameDataV0_0_1!
            if (previousVersionData is not GameDataV0_0_2 v2)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_2)}");

            var tileColors = new Dictionary<char, TileColorData>
            {
                ['.'] = new TileColorData { Foreground = (int)ConsoleColor.Green, Background = (int)ConsoleColor.Black },
                [','] = new TileColorData { Foreground = (int)ConsoleColor.Green, Background = (int)ConsoleColor.Black },
                ['\''] = new TileColorData { Foreground = (int)ConsoleColor.Green, Background = (int)ConsoleColor.Black },
                ['`'] = new TileColorData { Foreground = (int)ConsoleColor.Green, Background = (int)ConsoleColor.Black },
                [';'] = new TileColorData { Foreground = (int)ConsoleColor.Green, Background = (int)ConsoleColor.Black },
                ['"'] = new TileColorData { Foreground = (int)ConsoleColor.Yellow, Background = (int)ConsoleColor.Black },
                ['♣'] = new TileColorData { Foreground = (int)ConsoleColor.DarkGreen, Background = (int)ConsoleColor.Black },
                ['♠'] = new TileColorData { Foreground = (int)ConsoleColor.DarkGreen, Background = (int)ConsoleColor.Black },
                ['≈'] = new TileColorData { Foreground = (int)ConsoleColor.Cyan, Background = (int)ConsoleColor.Black },
                ['┌'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['┐'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['└'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['┘'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['─'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['│'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['┬'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['┴'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['├'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['┤'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['┼'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['╪'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['▒'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['#'] = new TileColorData { Foreground = (int)ConsoleColor.DarkYellow, Background = (int)ConsoleColor.Black },
                ['Θ'] = new TileColorData { Foreground = (int)ConsoleColor.Red, Background = (int)ConsoleColor.Black },
                ['≡'] = new TileColorData { Foreground = (int)ConsoleColor.White, Background = (int)ConsoleColor.DarkBlue },
                ['╔'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['╗'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['╚'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['╝'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['═'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['║'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['╠'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['╣'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['╦'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['╩'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
                ['╬'] = new TileColorData { Foreground = (int)ConsoleColor.Gray, Background = (int)ConsoleColor.Black },
            };

            return new GameDataV0_0_3
            {
                Map = (char[,])v2.Map.Clone(),
                PlayerX = v2.PlayerX,
                PlayerY = v2.PlayerY,
                TileColors = tileColors
            };
        }
    }

    public class ConverterV0_0_3ToV0_0_2 : IVersionConverter
    {
        public Version SourceVersion => new Version(0, 0, 3);
        public Version TargetVersion => new Version(0, 0, 2);
        public object Convert(object previousVersionData)
        {
            if (previousVersionData is not GameDataV0_0_3 v3)
                throw new ArgumentException($"Ожидался тип {nameof(GameDataV0_0_3)}");
            return new GameDataV0_0_2
            {
                Map = (char[,])v3.Map.Clone(),
                PlayerX = v3.PlayerX,
                PlayerY = v3.PlayerY
            };
        }
    }
}