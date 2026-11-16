// Data/FpsCounter.cs
using System.Diagnostics;

namespace AsciiFarm.Data;

/// <summary>Потокобезопасный счётчик: RecordFrame — render-поток, RecordTick — симуляция.</summary>
public sealed class FpsCounter
{
    private const int SampleSize = 60;

    private readonly object _gate = new();
    private readonly Queue<double> _frameTimes = new();
    private readonly Stopwatch _renderTimer = Stopwatch.StartNew();
    private readonly Stopwatch _tickTimer = Stopwatch.StartNew();

    private int _framesThisSecond;
    private int _ticksThisSecond;
    private float _currentFps;
    private float _averageFps;
    private int _realFps;
    private int _tps;

    public float CurrentFps { get { lock (_gate) return _currentFps; } }
    public float AverageFps { get { lock (_gate) return _averageFps; } }
    public int RealFps { get { lock (_gate) return _realFps; } }
    public int TicksPerSecond { get { lock (_gate) return _tps; } }

    /// <param name="timestampSeconds">Монотонное реальное время (Stopwatch), НЕ номер тика.</param>
    public void RecordFrame(double timestampSeconds)
    {
        lock (_gate)
        {
            _frameTimes.Enqueue(timestampSeconds);
            if (_frameTimes.Count > SampleSize) _frameTimes.Dequeue();

            if (_frameTimes.Count >= 2)
            {
                double duration = timestampSeconds - _frameTimes.Peek();   // без ToArray()
                if (duration > 0.001)
                    _currentFps = (float)((_frameTimes.Count - 1) / duration);
            }

            _framesThisSecond++;
            if (_renderTimer.ElapsedMilliseconds >= 1000)
            {
                _realFps = _framesThisSecond;
                _framesThisSecond = 0;
                _renderTimer.Restart();
            }
        }
    }

    public void RecordTick()
    {
        lock (_gate)
        {
            _ticksThisSecond++;
            if (_tickTimer.ElapsedMilliseconds >= 1000)
            {
                _tps = _ticksThisSecond;
                _ticksThisSecond = 0;
                _tickTimer.Restart();
            }
        }
    }

    public void UpdateAverage(float instantFps)
    {
        lock (_gate)
            _averageFps = _averageFps == 0 ? instantFps : _averageFps * 0.9f + instantFps * 0.1f;
    }
}