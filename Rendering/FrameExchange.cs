using AsciiFarm.Core;

namespace AsciiFarm.Rendering;

public sealed class FrameExchange<T> where T : class
{
    private T[] _buffers;  // Убрали readonly
    private readonly object _gate = new();
    private readonly ThreadGuard _writerGuard = new("FrameExchange.Writer");
    private readonly ThreadGuard _readerGuard = new("FrameExchange.Reader");

    private int _writeIndex;
    private int _readIndex = 1;
    private int _readyIndex = 2;
    private bool _hasFreshFrame;

    public FrameExchange(Func<T> factory) =>
        _buffers = new[] { factory(), factory(), factory() };

    public void AttachWriter() => _writerGuard.AttachToCurrentThread();
    public void AttachReader() => _readerGuard.AttachToCurrentThread();

    public T WriteBuffer
    {
        get { _writerGuard.Validate(); return _buffers[_writeIndex]; }
    }

    public void Publish()
    {
        _writerGuard.Validate();
        lock (_gate)
        {
            (_writeIndex, _readyIndex) = (_readyIndex, _writeIndex);
            _hasFreshFrame = true;
        }
    }

    public bool TryAcquireFresh(out T frame)
    {
        _readerGuard.Validate();
        lock (_gate)
        {
            if (_hasFreshFrame)
            {
                (_readIndex, _readyIndex) = (_readyIndex, _readIndex);
                _hasFreshFrame = false;
                frame = _buffers[_readIndex];
                return true;
            }
        }
        frame = _buffers[_readIndex];
        return false;
    }

    /// <summary>Пересоздаёт все буферы (при изменении размера окна).</summary>
    public void Resize(Func<T> factory)
    {
        lock (_gate)
        {
            _buffers = new[] { factory(), factory(), factory() };
            _writeIndex = 0;
            _readIndex = 1;
            _readyIndex = 2;
            _hasFreshFrame = false;
        }
    }
}