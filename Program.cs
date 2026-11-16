// Program.cs
using AsciiFarm.App;
namespace AsciiFarm;

internal static class Program
{
    private static void Main() => new GameApplication(new GameSettings()).Run();
}