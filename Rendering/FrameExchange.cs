// Rendering/FrameExchange.cs
using AsciiFarm.Core;
namespace AsciiFarm.Rendering;

/// <summary>
/// Тройная буферизация: write-буфер принадлежит симуляции, read-буфер — рендеру,
/// средний слот меняется только под lock. Одновременного доступа к содержимому нет физически.
/// </summary>
public sealed class FrameExchange<T> where T : class
{
    private readonly T[] _buffers;
    private readonly object _gate = new();
    private readonly ThreadGuard _writerGuard = new("FrameExchange.Writer");
    private readonly ThreadGuard _readerGuard = new("FrameExchange.Reader");

    private int _writeIndex;        // трогает только писатель
    private int _readIndex = 1;     // трогает только читатель
    private int _readyIndex = 2;    // только под _gate
    private bool _hasFreshFrame;    // флаг готовности, только под _gate

    public FrameExchange(Func<T> factory) => _buffers = new[] { factory(), factory(), factory() };

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
}