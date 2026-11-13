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
        var state = world.Resource<GameState>();
        var commands = world.Resource<CommandBuffer>().Items;
        var intents = world.Store<MoveIntent>();
        var players = world.Query<PlayerTag, Position>();

        foreach (var command in commands)
        {
            if (command == GameCommand.Exit)
            {
                state.IsRunning = false;
                state.NeedsSave = true;
                continue;
            }

            var (dx, dy) = command switch
            {
                GameCommand.MoveUp => (0, -1),
                GameCommand.MoveDown => (0, 1),
                GameCommand.MoveLeft => (-1, 0),
                GameCommand.MoveRight => (1, 0),
                _ => (0, 0)
            };

            if (dx == 0 && dy == 0) continue;

            foreach (var entity in players)
                intents.Set(entity, new MoveIntent(dx, dy));
        }

        // Обработка команд управления временем
        foreach (var command in commands)
        {
            switch (command)
            {
                // Относительные коэффициенты к DefaultTimeSpeed
                case GameCommand.Speed1: state.TimeSpeed = state.DefaultTimeSpeed; break;
                case GameCommand.Speed2: state.TimeSpeed = state.DefaultTimeSpeed * 2; break;
                case GameCommand.Speed3: state.TimeSpeed = state.DefaultTimeSpeed * 3; break;
                case GameCommand.Speed4: state.TimeSpeed = state.DefaultTimeSpeed * 4; break;
                case GameCommand.Speed5: state.TimeSpeed = state.DefaultTimeSpeed * 5; break;

                // Абсолютные значения для перемотки (не меняются)
                case GameCommand.FastForward1: state.FastForwardUntil = state.GameTimeElapsed + 3600.0; break;
                case GameCommand.FastForward2: state.FastForwardUntil = state.GameTimeElapsed + 86400.0; break;
                case GameCommand.FastForward3: state.FastForwardUntil = state.GameTimeElapsed + 604800.0; break;
                case GameCommand.FastForward4: state.FastForwardUntil = state.GameTimeElapsed + 2592000.0; break;
                case GameCommand.FastForward5: state.FastForwardUntil = state.GameTimeElapsed + 31536000.0; break;
            }
        }

        commands.Clear();
    }
}