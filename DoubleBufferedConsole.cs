using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace askiifarm
{


    public sealed class DoubleBufferedConsole : IDisposable
    {
        private const int STD_OUTPUT_HANDLE = -11;

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint CONSOLE_TEXTMODE_BUFFER = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CHAR_INFO
        {
            public char UnicodeChar;
            public short Attributes;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern COORD GetLargestConsoleWindowSize(SafeFileHandle hConsoleOutput);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleScreenBufferSize(
            SafeFileHandle hConsoleOutput,
            COORD dwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleWindowInfo(
            SafeFileHandle hConsoleOutput,
            [MarshalAs(UnmanagedType.Bool)] bool bAbsolute,
            ref SMALL_RECT lpConsoleWindow);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteConsoleOutput(
            SafeFileHandle hConsoleOutput,
            CHAR_INFO[] lpBuffer,
            COORD dwBufferSize,
            COORD dwBufferCoord,
            ref SMALL_RECT lpConsoleWriteRegion);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateConsoleScreenBuffer(
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwFlags,
            IntPtr lpScreenBufferData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleActiveScreenBuffer(
            SafeFileHandle hConsoleOutput);

        private readonly SafeFileHandle _stdout;

        private SafeFileHandle _front;
        private SafeFileHandle _back;

        private CHAR_INFO[] _cells;

        private int _width;
        private int _height;

        private bool _dirty;
        private bool _disposed;

        public int Width => _width;
        public int Height => _height;

        /// <summary>
        /// Цвет текста по умолчанию для Clear().
        /// </summary>
        public ConsoleColor DefaultForeground { get; set; }

        /// <summary>
        /// Цвет фона по умолчанию для Clear().
        /// </summary>
        public ConsoleColor DefaultBackground { get; set; }

        public DoubleBufferedConsole(
            int width,
            int height,
            ConsoleColor defaultForeground = ConsoleColor.Gray,
            ConsoleColor defaultBackground = ConsoleColor.Black)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Поддерживается только Windows.");

            if (Console.IsOutputRedirected)
                throw new InvalidOperationException("Вывод консоли перенаправлен. Нужна настоящая консоль.");

            ValidateSize(width, height);

            _width = width;
            _height = height;

            DefaultForeground = defaultForeground;
            DefaultBackground = defaultBackground;

            _stdout = OpenStandardOutput();

            try
            {
                ValidateAgainstLargestWindowSize(width, height);

                _front = CreateBuffer(width, height);
                _back = CreateBuffer(width, height);

                _cells = new CHAR_INFO[width * height];

                FillCells(
                    _cells,
                    ' ',
                    MakeAttribute(DefaultForeground, DefaultBackground));

                // Показываем начальный пустой кадр.
                WriteBuffer(_front, _cells, width, height);
                SetActiveBuffer(_front);

                _dirty = false;

                try
                {
                    Console.CursorVisible = false;
                }
                catch
                {
                    // Не критично, если консоль не позволяет управлять курсором.
                }
            }
            catch
            {
                _front?.Dispose();
                _back?.Dispose();
                _stdout.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Добавляет один символ в буфер.
        /// Если координаты вне диапазона, вызов игнорируется.
        /// </summary>
        public void PutChar(
            int x,
            int y,
            char character,
            ConsoleColor foreground,
            ConsoleColor background)
        {
            CheckNotDisposed();

            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                return;

            int index = y * _width + x;

            _cells[index].UnicodeChar = character;
            _cells[index].Attributes = MakeAttribute(foreground, background);

            _dirty = true;
        }

        /// <summary>
        /// Рисует текущий буфер.
        /// Если изменений не было, ничего не делает, если не задан force = true.
        /// </summary>
        public void Draw(bool force = false)
        {
            CheckNotDisposed();

            if (!_dirty && !force)
                return;

            // Пишем кадр в скрытый буфер и делаем его активным.
            WriteBuffer(_back, _cells, _width, _height);
            SetActiveBuffer(_back);

            SafeFileHandle temp = _front;
            _front = _back;
            _back = temp;

            _dirty = false;
        }

        /// <summary>
        /// Очищает буфер пробелами и цветами по умолчанию.
        /// </summary>
        public void Clear()
        {
            CheckNotDisposed();
            Fill(' ', DefaultForeground, DefaultBackground);
        }

        /// <summary>
        /// Заливает весь буфер одним символом и цветами.
        /// </summary>
        public void Fill(
            char character,
            ConsoleColor foreground,
            ConsoleColor background)
        {
            CheckNotDisposed();

            short attributes = MakeAttribute(foreground, background);
            FillCells(_cells, character, attributes);

            _dirty = true;
        }

        /// <summary>
        /// Заливает прямоугольник.
        /// Координаты — верхний левый угол.
        /// Прямоугольник автоматически обрезается по границам буфера.
        /// </summary>
        public void FillRect(
            int x,
            int y,
            int width,
            int height,
            char character,
            ConsoleColor foreground,
            ConsoleColor background)
        {
            CheckNotDisposed();

            if (width < 0 || height < 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Размер не может быть отрицательным.");

            if (width == 0 || height == 0)
                return;

            int left = Math.Max(0, x);
            int top = Math.Max(0, y);

            int right = (int)Math.Min((long)_width, (long)x + width);
            int bottom = (int)Math.Min((long)_height, (long)y + height);

            if (left >= right || top >= bottom)
                return;

            short attributes = MakeAttribute(foreground, background);

            for (int yy = top; yy < bottom; yy++)
            {
                int rowStart = yy * _width;

                for (int xx = left; xx < right; xx++)
                {
                    int index = rowStart + xx;

                    _cells[index].UnicodeChar = character;
                    _cells[index].Attributes = attributes;
                }
            }

            _dirty = true;
        }

        /// <summary>
        /// Меняет размер буфера и окна консоли.
        /// Текущий кадр при этом сбрасывается.
        /// </summary>
        public void Resize(int width, int height)
        {
            CheckNotDisposed();
            ValidateSize(width, height);

            if (width == _width && height == _height)
                return;

            ValidateAgainstLargestWindowSize(width, height);

            SafeFileHandle newFront = CreateBuffer(width, height);
            SafeFileHandle newBack = null;

            try
            {
                newBack = CreateBuffer(width, height);

                CHAR_INFO[] newCells = new CHAR_INFO[width * height];

                FillCells(
                    newCells,
                    ' ',
                    MakeAttribute(DefaultForeground, DefaultBackground));

                WriteBuffer(newFront, newCells, width, height);
                SetActiveBuffer(newFront);

                SafeFileHandle oldFront = _front;
                SafeFileHandle oldBack = _back;

                _front = newFront;
                _back = newBack;
                _cells = newCells;

                _width = width;
                _height = height;

                _dirty = false;

                oldFront?.Dispose();
                oldBack?.Dispose();
            }
            catch
            {
                newFront.Dispose();
                newBack?.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                // Возвращаем исходный консольный буфер.
                if (_stdout != null && !_stdout.IsInvalid)
                    SetConsoleActiveScreenBuffer(_stdout);
            }
            catch
            {
                // Если не удалось вернуть исходный буфер, игнорируем.
            }

            _front?.Dispose();
            _back?.Dispose();
            _stdout?.Dispose();

            try
            {
                Console.CursorVisible = true;
            }
            catch
            {
                // Не критично.
            }
        }

        private void CheckNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DoubleBufferedConsole));
        }

        private static void ValidateSize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Размеры должны быть положительными.");

            if (width > short.MaxValue || height > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(width), "Размеры слишком большие для COORD.");

            if ((long)width * height > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(width), "Слишком много ячеек.");
        }

        private void ValidateAgainstLargestWindowSize(int width, int height)
        {
            COORD largest = GetLargestConsoleWindowSize(_stdout);

            if (largest.X > 0 && largest.Y > 0 && (width > largest.X || height > largest.Y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Размер окна консоли не может быть больше {largest.X}x{largest.Y}.");
            }
        }

        private static SafeFileHandle OpenStandardOutput()
        {
            SafeFileHandle handle = new SafeFileHandle(GetStdHandle(STD_OUTPUT_HANDLE), false);

            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return handle;
        }

        private static SafeFileHandle CreateBuffer(int width, int height)
        {
            SafeFileHandle handle = CreateConsoleScreenBuffer(
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                CONSOLE_TEXTMODE_BUFFER,
                IntPtr.Zero);

            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            try
            {
                COORD size = new COORD
                {
                    X = (short)width,
                    Y = (short)height
                };

                SMALL_RECT target = new SMALL_RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = (short)(width - 1),
                    Bottom = (short)(height - 1)
                };

                // Если окно можно сразу сделать нужного размера — делаем.
                // Если нет, сначала уменьшаем до 1x1, затем меняем размер буфера.
                if (!SetConsoleWindowInfo(handle, true, ref target))
                {
                    SMALL_RECT tiny = new SMALL_RECT
                    {
                        Left = 0,
                        Top = 0,
                        Right = 0,
                        Bottom = 0
                    };

                    if (!SetConsoleWindowInfo(handle, true, ref tiny))
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    if (!SetConsoleScreenBufferSize(handle, size))
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    if (!SetConsoleWindowInfo(handle, true, ref target))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                else
                {
                    if (!SetConsoleScreenBufferSize(handle, size))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return handle;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        private static void WriteBuffer(
            SafeFileHandle handle,
            CHAR_INFO[] cells,
            int width,
            int height)
        {
            COORD bufferSize = new COORD
            {
                X = (short)width,
                Y = (short)height
            };

            COORD bufferCoord = new COORD
            {
                X = 0,
                Y = 0
            };

            SMALL_RECT region = new SMALL_RECT
            {
                Left = 0,
                Top = 0,
                Right = (short)(width - 1),
                Bottom = (short)(height - 1)
            };

            if (!WriteConsoleOutput(handle, cells, bufferSize, bufferCoord, ref region))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private static void SetActiveBuffer(SafeFileHandle handle)
        {
            if (!SetConsoleActiveScreenBuffer(handle))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private static void FillCells(
            CHAR_INFO[] cells,
            char character,
            short attributes)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].UnicodeChar = character;
                cells[i].Attributes = attributes;
            }
        }

        private static short MakeAttribute(ConsoleColor foreground, ConsoleColor background)
        {
            int fg = (int)foreground;
            int bg = (int)background;

            if ((fg & ~0xF) != 0 || (bg & ~0xF) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(foreground),
                    "ConsoleColor должен быть в диапазоне 0..15.");
            }

            return (short)(fg | (bg << 4));
        }
    }
}
