// Core/FpsLimiter.cs
using System.Diagnostics;
namespace AsciiFarm.Core;

/// <summary>
/// Ограничитель частоты кадров через дельта-время.
/// </summary>
public sealed class FpsLimiter
{
    private readonly double _targetFrameTimeSeconds;
    private readonly Stopwatch _stopwatch = new();
    private double _lastAccumulatedTime;

    public int TargetFps { get; }
    public float DeltaTime { get; private set; }
    public double ElapsedTime => _stopwatch.Elapsed.TotalSeconds;

    public FpsLimiter(int targetFps)
    {
        TargetFps = targetFps > 0 ? targetFps
            : throw new ArgumentOutOfRangeException(nameof(targetFps));
        _targetFrameTimeSeconds = 1.0 / targetFps;
    }

    /// <summary>Возвращает true, когда пора делать тик.</summary>
    public bool BeginTick()
    {
        if (!_stopwatch.IsRunning) _stopwatch.Start();

        double now = _stopwatch.Elapsed.TotalSeconds;
        double delta = now - _lastAccumulatedTime;

        if (delta < _targetFrameTimeSeconds * 0.95) return false;

        DeltaTime = (float)Math.Min(delta, _targetFrameTimeSeconds * 2);
        _lastAccumulatedTime = now;
        return true;
    }
}