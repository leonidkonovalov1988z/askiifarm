// Systems/PlayerIntentSystem.cs
using AsciiFarm.Components;
using AsciiFarm.Core;
using AsciiFarm.Data;
using AsciiFarm.Input;
namespace AsciiFarm.Systems;

/// <summary>Команды → намерение движения. Про клавиши больше никто не знает.</summary>
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
            if (command == GameCommand.Exit) { state.IsRunning = false; continue; }

            var (dx, dy) = command switch
            {
                GameCommand.MoveUp => (0, -1),
                GameCommand.MoveDown => (0, 1),
                GameCommand.MoveLeft => (-1, 0),
                GameCommand.MoveRight => (1, 0),
                _ => (0, 0)
            };
            if (dx == 0 && dy == 0) continue;

            foreach (var entity in players) intents.Set(entity, new MoveIntent(dx, dy));
        }

        commands.Clear();
    }
}