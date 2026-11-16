// Systems/WeatherSystem.cs
using AsciiFarm.Core;
using AsciiFarm.Data;

namespace AsciiFarm.Systems;

[GameSystem(Order = 6)]
public sealed class WeatherSystem : ISystem
{
    public void Update(World world)
    {
        var source = world.Resource<WeatherDataSource>();
        if (source.HourlyData.Count == 0) return;

        var weather = world.Resource<WeatherCurrent>();
        var state = world.Resource<GameState>();
        var sys = world.Resource<WeatherSystemState>();

        int hoursPassed = (int)(state.GameTimeElapsed / 3600.0);
        int index = (source.StartIndex + hoursPassed) % source.HourlyData.Count;
        if (index == sys.LastWeatherIndex) return;

        weather.Current = source.HourlyData[index];
        weather.HourIndex = index;
        weather.HasData = true;
        weather.GameTime = state.GameStartDate.AddSeconds(state.GameTimeElapsed);
        sys.LastWeatherIndex = index;
    }
}