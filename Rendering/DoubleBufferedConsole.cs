using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace askiifarm.Rendering
{
    public sealed class DoubleBufferedConsole : IDisposable
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint CONSOLE_TEXTMODE_BUFFER = 1;

        private readonly AsciiFarm.Core.ThreadGuard _guard = new("Console");
        public void AttachToCurrentThread() => _guard.AttachToCurrentThread();

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD { public short X; public short Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        private struct CHAR_INFO
        {
            [FieldOffset(0)] public char UnicodeChar;
            [FieldOffset(0)] public byte AsciiChar;
            [FieldOffset(2)] public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern COORD GetLargestConsoleWindowSize(SafeFileHandle hConsoleOutput);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleScreenBufferSize(SafeFileHandle hConsoleOutput, COORD dwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleWindowInfo(
            SafeFileHandle hConsoleOutput,
            [MarshalAs(UnmanagedType.Bool)] bool bAbsolute,
            ref SMALL_RECT lpConsoleWindow);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleScreenBufferInfo(
            SafeFileHandle hConsoleOutput,
            out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

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
            nint lpSecurityAttributes,
            uint dwFlags,
            nint lpScreenBufferData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleActiveScreenBuffer(SafeFileHandle hConsoleOutput);

        private readonly SafeFileHandle _stdout;
        private SafeFileHandle _buffer;
        private CHAR_INFO[] _cells;
        private int _width;
        private int _height;
        private bool _dirty;
        private bool _disposed;

        public int Width => _width;
        public int Height => _height;
        public ConsoleColor DefaultForeground { get; set; }
        public ConsoleColor DefaultBackground { get; set; }

        public DoubleBufferedConsole(
            int width, int height,
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
                _buffer = CreateBuffer();

                // КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: буфер должен быть активным ДО работы с его окном.
                // SetConsoleWindowInfo на неактивном буфере возвращает ERROR_INVALID_PARAMETER.
                SetActiveBuffer(_buffer);

                SetBufferSize(width, height);

                _cells = new CHAR_INFO[width * height];
                FillCells(_cells, ' ', MakeAttribute(DefaultForeground, DefaultBackground));
                WriteBuffer(_buffer, _cells, width, height);
                _dirty = false;

                try { Console.CursorVisible = false; } catch { }
            }
            catch
            {
                _buffer?.Dispose();
                _stdout.Dispose();
                throw;
            }
        }

        public void PutChar(int x, int y, char character, ConsoleColor foreground, ConsoleColor background)
        {
            _guard.Validate();
            CheckNotDisposed();
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) return;
            int index = y * _width + x;
            _cells[index].UnicodeChar = character;
            _cells[index].Attributes = MakeAttribute(foreground, background);
            _dirty = true;
        }

        public void Draw(bool force = false)
        {
            _guard.Validate();
            CheckNotDisposed();
            if (!_dirty && !force) return;
            WriteBuffer(_buffer, _cells, _width, _height);
            _dirty = false;
        }

        public void Clear()
        {
            _guard.Validate();
            CheckNotDisposed();
            Fill(' ', DefaultForeground, DefaultBackground);
        }

        public void Fill(char character, ConsoleColor foreground, ConsoleColor background)
        {
            _guard.Validate();
            CheckNotDisposed();
            short attributes = MakeAttribute(foreground, background);
            FillCells(_cells, character, attributes);
            _dirty = true;
        }

        public void FillRect(int x, int y, int width, int height, char character,
            ConsoleColor foreground, ConsoleColor background)
        {
            _guard.Validate();
            CheckNotDisposed();
            if (width < 0 || height < 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Размер не может быть отрицательным.");
            if (width == 0 || height == 0) return;

            int left = Math.Max(0, x);
            int top = Math.Max(0, y);
            int right = (int)Math.Min(_width, (long)x + width);
            int bottom = (int)Math.Min(_height, (long)y + height);
            if (left >= right || top >= bottom) return;

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

        public void Resize(int width, int height)
        {
            _guard.Validate();
            CheckNotDisposed();
            ValidateSize(width, height);
            if (width == _width && height == _height) return;
            ValidateAgainstLargestWindowSize(width, height);

            SetBufferSize(width, height);

            _cells = new CHAR_INFO[width * height];
            FillCells(_cells, ' ', MakeAttribute(DefaultForeground, DefaultBackground));
            _width = width;
            _height = height;

            // Сразу затираем весь буфер пробелами, не дожидаясь свежего кадра.
            // Это убирает мусор, который conhost оставляет в новых ячейках при растяжении.
            WriteBuffer(_buffer, _cells, width, height);
            _dirty = false;
        }

        public (int Width, int Height) GetWindowSize()
        {
            _guard.Validate();
            CheckNotDisposed();
            if (GetConsoleScreenBufferInfo(_buffer, out var info))
            {
                int w = info.srWindow.Right - info.srWindow.Left + 1;
                int h = info.srWindow.Bottom - info.srWindow.Top + 1;
                return (w, h);
            }
            return (_width, _height);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_stdout != null && !_stdout.IsInvalid)
                    SetConsoleActiveScreenBuffer(_stdout);
            }
            catch { }

            _buffer?.Dispose();
            _stdout?.Dispose();

            try { Console.CursorVisible = true; } catch { }
        }

        private void CheckNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DoubleBufferedConsole));
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
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Размер окна консоли не может быть больше {largest.X}x{largest.Y}.");
        }

        private static SafeFileHandle OpenStandardOutput()
        {
            SafeFileHandle handle = new SafeFileHandle(GetStdHandle(STD_OUTPUT_HANDLE), false);
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            return handle;
        }

        private SafeFileHandle CreateBuffer()
        {
            SafeFileHandle handle = CreateConsoleScreenBuffer(
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                nint.Zero,
                CONSOLE_TEXTMODE_BUFFER,
                nint.Zero);
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            return handle;
        }

        /// <summary>
        /// Устанавливает размер буфера и окна.
        /// Соблюдает инвариант Windows: размер окна не может превышать размер буфера.
        /// Никаких «трюков» с 1×1 — только корректная последовательность.
        /// </summary>
        private void SetBufferSize(int width, int height)
        {
            if (!GetConsoleScreenBufferInfo(_buffer, out var info))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            int curBufW = info.dwSize.X;
            int curBufH = info.dwSize.Y;
            int curWinW = info.srWindow.Right - info.srWindow.Left + 1;
            int curWinH = info.srWindow.Bottom - info.srWindow.Top + 1;

            COORD size = new COORD { X = (short)width, Y = (short)height };
            SMALL_RECT target = new SMALL_RECT
            {
                Left = 0,
                Top = 0,
                Right = (short)(width - 1),
                Bottom = (short)(height - 1)
            };

            // Если уменьшаем буфер, а текущее окно больше нового буфера —
            // сначала уменьшаем окно до целевого размера (это всегда разрешено).
            if (width < curWinW || height < curWinH)
            {
                SMALL_RECT interim = target;
                if (width > curBufW) interim.Right = (short)(curBufW - 1);
                if (height > curBufH) interim.Bottom = (short)(curBufH - 1);
                if (!SetConsoleWindowInfo(_buffer, true, ref interim))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Меняем размер буфера.
            if (!SetConsoleScreenBufferSize(_buffer, size))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Устанавливаем финальный размер окна.
            if (!SetConsoleWindowInfo(_buffer, true, ref target))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private static void WriteBuffer(SafeFileHandle handle, CHAR_INFO[] cells, int width, int height)
        {
            COORD bufferSize = new COORD { X = (short)width, Y = (short)height };
            COORD bufferCoord = new COORD { X = 0, Y = 0 };
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

        private static void FillCells(CHAR_INFO[] cells, char character, short attributes)
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
                throw new ArgumentOutOfRangeException(nameof(foreground), "ConsoleColor должен быть в диапазоне 0..15.");
            return (short)(fg | bg << 4);
        }
    }
}