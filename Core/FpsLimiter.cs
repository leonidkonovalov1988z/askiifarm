// Core/FpsLimiter.cs
using System.Diagnostics;
namespace AsciiFarm.Core;

public sealed class FpsLimiter
{
    private double _targetFrameTimeSeconds;
    private readonly Stopwatch _stopwatch = new();
    private double _lastAccumulatedTime;

    public int TargetFps { get; private set; }
    public float DeltaTime { get; private set; }
    public double ElapsedTime => _stopwatch.Elapsed.TotalSeconds;

    public FpsLimiter(int targetFps)
    {
        TargetFps = targetFps > 0 ? targetFps
            : throw new ArgumentOutOfRangeException(nameof(targetFps));
        _targetFrameTimeSeconds = 1.0 / targetFps;
    }

    /// <summary>Изменить целевой FPS на лету (для ускорения времени).</summary>
    public void SetTargetFps(int targetFps)
    {
        if (targetFps <= 0) return;
        TargetFps = targetFps;
        _targetFrameTimeSeconds = 1.0 / targetFps;
        // Сбрасываем накопитель, чтобы не было скачка
        _lastAccumulatedTime = _stopwatch.Elapsed.TotalSeconds;
    }

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