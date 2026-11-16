// Rendering/FrameExchange.cs
using AsciiFarm.Core;

namespace AsciiFarm.Rendering;

public sealed class FrameExchange<T> where T : class
{
    private readonly object _gate = new();
    private readonly ThreadGuard _writerGuard = new("FrameExchange.Writer");
    private readonly ThreadGuard _readerGuard = new("FrameExchange.Reader");

    private T[] _buffers;
    private int _writeIndex;
    private int _readIndex = 1;
    private int _readyIndex = 2;
    private bool _hasFreshFrame;

    private Func<T>? _pendingResize;   // защищён _gate; применяет только писатель

    public FrameExchange(Func<T> factory) =>
        _buffers = new[] { factory(), factory(), factory() };

    public void AttachWriter() => _writerGuard.AttachToCurrentThread();
    public void AttachReader() => _readerGuard.AttachToCurrentThread();

    public T WriteBuffer
    {
        get
        {
            _writerGuard.Validate();
            ApplyPendingResize();                 // безопасная точка: кадр ещё не начат
            lock (_gate) return _buffers[_writeIndex];
        }
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
            frame = _buffers[_readIndex];
            return false;
        }
    }

    /// <summary>Заявка на смену размера (вызывается из render-потока).</summary>
    public void Resize(Func<T> factory)
    {
        lock (_gate) _pendingResize = factory;
    }

    private void ApplyPendingResize()
    {
        Func<T>? factory;
        lock (_gate)
        {
            factory = _pendingResize;
            if (factory == null) return;
            _pendingResize = null;
        }

        // Аллокация вне замка; _buffers меняет только писатель.
        var newBuffers = new[] { factory(), factory(), factory() };

        lock (_gate)
        {
            _buffers = newBuffers;
            _writeIndex = 0;
            _readIndex = 1;
            _readyIndex = 2;
            _hasFreshFrame = false;
        }
    }
}