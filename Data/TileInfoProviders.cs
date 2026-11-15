using System.Collections.Generic;
using AsciiFarm.Components;
using AsciiFarm.Core;

namespace AsciiFarm.Data;

/// <summary>Информация о стиле тайла из палитры.</summary>
public sealed class TilePaletteInfoProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(char tile, World world)
    {
        var palette = world.Resource<TilePalette>();
        var style = palette.Resolve(tile);

        yield return $"Символ: '{tile}'";
        yield return $"Цвет: {style.Foreground}";
        yield return $"Фон: {style.Background}";
        yield return $"Проходимость: {(style.BlocksMovement ? "Нет" : "Да")}";
    }
}

/// <summary>Информация о погоде в точке тайла.</summary>
public sealed class WeatherTileInfoProvider : ITileInfoProvider
{
    public IEnumerable<string> GetInfo(char tile, World world)
    {
        var weather = world.Resource<WeatherCurrent>();
        if (!weather.HasData) yield break;

        var d = weather.Current;
        yield return $"Темп: {d.AirTemp:F1}°C";
        yield return $"Влажн: {d.AirHumidity * 100:F0}%";
        yield return $"Почва: {d.SoilMoisture * 100:F0}%";
        yield return $"Снег: {d.EffectiveSnowCm:F1}см";
    }
}