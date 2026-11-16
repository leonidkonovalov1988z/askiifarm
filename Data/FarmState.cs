// Data/FarmState.cs
namespace AsciiFarm.Data;

public sealed class FarmSelection
{
    public int Index { get; set; }
}

public sealed class FarmLog
{
    public string Message { get; set; } = "";
    public double ShowUntil { get; set; }

    public void Set(string message, double nowSeconds, double seconds = 6.0)
    {
        Message = message;
        ShowUntil = nowSeconds + seconds;
    }

    public string? Current(double nowSeconds) => nowSeconds < ShowUntil ? Message : null;
}