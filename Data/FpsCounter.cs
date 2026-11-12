// Data/FpsCounter.cs
using System.Collections.Generic;
namespace AsciiFarm.Data;

/// <summary>Счётчик FPS (скользящее окно + экспоненциальное сглаживание).</summary>
public sealed class FpsCounter
{
    private const int SampleSize = 60;
    private readonly Queue<double> _frameTimes = new();
    private readonly object _gate = new();

    public float CurrentFps { get; private set; }
    public float AverageFps { get; private set; }

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
        }
    }

    public void UpdateAverage(float instantFps)
    {
        lock (_gate)
        {
            AverageFps = AverageFps == 0
                ? instantFps
                : AverageFps * 0.9f + instantFps * 0.1f;
        }
    }
}