// Systems/WeatherSystem.cs
using System;
using AsciiFarm.Core;
using AsciiFarm.Data;
namespace AsciiFarm.Systems;

[GameSystem(Order = 6)]
public sealed class WeatherSystem : ISystem
{
    private int _lastWeatherIndex = -1;

    public void Update(World world)
    {
        var weather = world.Resource<WeatherCurrent>();
        var source = world.Resource<WeatherDataSource>();
        var state = world.Resource<GameState>();

        if (source.HourlyData.Count == 0) return;

        // Вычисляем индекс погоды: StartIndex + количество игровых часов, по модулю длины CSV
        int hoursPassed = (int)(state.GameTimeElapsed / 3600.0);
        int weatherIndex = (source.StartIndex + hoursPassed) % source.HourlyData.Count;

        if (weatherIndex != _lastWeatherIndex)
        {
            weather.Current = source.HourlyData[weatherIndex];
            weather.HourIndex = weatherIndex;
            weather.HasData = true;
            // Игровое время = GameStartDate + GameTimeElapsed
            weather.GameTime = state.GameStartDate.AddSeconds(state.GameTimeElapsed);
            _lastWeatherIndex = weatherIndex;
        }
    }
}