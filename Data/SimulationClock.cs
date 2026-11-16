// Data/SimulationClock.cs
using System.Diagnostics;

namespace AsciiFarm.Data;

/// <summary>Ресурс мира: источник реальной дельты. Первый тик даёт 0, а не «время с постройки мира».</summary>
public sealed class SimulationClock
{
    private readonly Stopwatch _timer = new();
    private double _last;
    private bool _started;

    public double NextDelta()
    {
        if (!_started)
        {
            _started = true;
            _timer.Start();
            _last = 0;
            return 0.0;
        }
        double now = _timer.Elapsed.TotalSeconds;
        double delta = now - _last;
        _last = now;
        return delta;
    }
}