// Systems/PlayerIntentSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Input;

namespace AsciiFarm.Systems;

[GameSystem(Order = 3)]
public sealed class PlayerIntentSystem : ISystem
{
    public void Update(World world)
    {
        var commands = world.Resource<CommandBuffer>().Items;
        if (commands.Count == 0) return;

        var state = world.Resource<GameState>();
        var intents = world.Store<MoveIntent>();
        var farmIntents = world.Store<FarmIntent>();
        Entity[]? players = null;

        foreach (var command in commands)   // один проход
        {
            switch (command)
            {
                case GameCommand.Exit:
                    state.IsRunning = false;
                    state.NeedsSave = true;
                    break;

                case GameCommand.MoveUp: Move(0, -1); break;
                case GameCommand.MoveDown: Move(0, 1); break;
                case GameCommand.MoveLeft: Move(-1, 0); break;
                case GameCommand.MoveRight: Move(1, 0); break;
                case GameCommand.Plant: Farm(FarmAction.Plant); break;
                case GameCommand.Harvest: Farm(FarmAction.Harvest); break;
                case GameCommand.CycleCrop: Farm(FarmAction.CycleCrop); break;

                // Относительные скорости
                case GameCommand.Speed1: state.TimeSpeed = state.DefaultTimeSpeed; break;
                case GameCommand.Speed2: state.TimeSpeed = state.DefaultTimeSpeed * 10; break;

                // Абсолютные: сутки / месяц / год ИГРОВОГО календаря за реальную минуту
                case GameCommand.Speed3:
                    state.TimeSpeed = CalendarHelper.SecondsPerDay / CalendarHelper.SecondsPerMinute;
                    break;
                case GameCommand.Speed4:
                    state.TimeSpeed = MonthSeconds(state) / CalendarHelper.SecondsPerMinute;
                    break;
                case GameCommand.Speed5:
                    state.TimeSpeed = YearSeconds(state) / CalendarHelper.SecondsPerMinute;
                    break;

                // Промотка: час / сутки / неделя / месяц / год ИГРОВОГО календаря
                case GameCommand.FastForward1: Forward(CalendarHelper.SecondsPerHour); break;
                case GameCommand.FastForward2: Forward(CalendarHelper.SecondsPerDay); break;
                case GameCommand.FastForward3: Forward(7 * CalendarHelper.SecondsPerDay); break;
                case GameCommand.FastForward4: Forward(MonthSeconds(state)); break;
                case GameCommand.FastForward5: Forward(YearSeconds(state)); break;
                case GameCommand.PlantYear2: Farm(FarmAction.PlantYear2); break;
            }
        }

        commands.Clear();

        void Move(int dx, int dy)
        {
            players ??= world.Query<PlayerTag, Position>();
            foreach (var entity in players) intents.Set(entity, new MoveIntent(dx, dy));
        }

        void Farm(FarmAction action)
        {
            players ??= world.Query<PlayerTag, Position>();
            foreach (var entity in players) farmIntents.Set(entity, new FarmIntent(action));
        }

        void Forward(double seconds) => state.FastForwardUntil = state.GameTimeElapsed + seconds;
    }

    private static double MonthSeconds(GameState s) =>
        CalendarHelper.GetMonthSeconds(s.GameStartDate, s.GameTimeElapsed, s.DaysPerMonth);

    private static double YearSeconds(GameState s) =>
        CalendarHelper.GetYearSeconds(s.GameStartDate, s.GameTimeElapsed, s.DaysPerMonth);
}