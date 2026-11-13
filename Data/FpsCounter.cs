// Data/FpsCounter.cs
using System.Collections.Generic;
using System.Diagnostics;
namespace AsciiFarm.Data;

public sealed class FpsCounter
{
    private const int SampleSize = 60;
    private readonly Queue<double> _frameTimes = new();
    private readonly object _gate = new();

    // Для реального FPS (рендер)
    private int _framesThisSecond = 0;
    private readonly Stopwatch _renderTimer = Stopwatch.StartNew();

    // Для TPS (симуляция)
    private int _ticksThisSecond = 0;
    private readonly Stopwatch _tickTimer = Stopwatch.StartNew();

    public float CurrentFps { get; private set; }
    public float AverageFps { get; private set; }
    public int RealFps { get; private set; }
    public int TicksPerSecond { get; private set; }

    public void RecordFrame(double timestampSeconds)
    {
        lock (_gate)
        {
            _frameTimes.Enqueue(timestampSeconds);
            if (_frameTimes.Count > SampleSize) _frameTimes.Dequeue();

            if (_frameTimes.Count >= 2)
            {
                var first = _frameTimes.Peek();
                var last = _frameTimes.ToArray()[^1];
                double duration = last - first;
                if (duration > 0.001)
                    CurrentFps = (float)((_frameTimes.Count - 1) / duration);
            }

            // Считаем реальный FPS (рендер)
            _framesThisSecond++;
            if (_renderTimer.ElapsedMilliseconds >= 1000)
            {
                RealFps = _framesThisSecond;
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
                TicksPerSecond = _ticksThisSecond;
                _ticksThisSecond = 0;
                _tickTimer.Restart();
            }
        }
    }

    public void UpdateAverage(float instantFps)
    {
        lock (_gate)
        {
            AverageFps = AverageFps == 0 ? instantFps : AverageFps * 0.9f + instantFps * 0.1f;
        }
    }
}