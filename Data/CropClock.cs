// Data/CropClock.cs
namespace AsciiFarm.Data;

/// <summary>Сколько целых игровых часов уже прожито растениями.</summary>
public sealed class CropClock
{
    public long LastHour { get; set; } = -1;   // -1 = не инициализировано (первый тик без роста)
}