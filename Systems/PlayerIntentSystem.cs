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

        // Сбрасываем все предыдущие намерения
        foreach (var entity in intents.Entities.ToArray())
            intents.Remove(entity);

        // Накапливаем вектор движения из всех команд за тик
        int totalDx = 0;
        int totalDy = 0;
        bool exitRequested = false;

        foreach (var command in commands)
        {
            if (command == GameCommand.Exit)
            {
                exitRequested = true;
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
            totalDx += dx;
            totalDy += dy;
        }

        // Ограничиваем каждый компонент значением [-1, 1]
        totalDx = Math.Clamp(totalDx, -1, 1);
        totalDy = Math.Clamp(totalDy, -1, 1);

        // Если есть движение, устанавливаем намерение
        if (totalDx != 0 || totalDy != 0)
        {
            foreach (var entity in players)
                intents.Set(entity, new MoveIntent(totalDx, totalDy));
        }

        if (exitRequested)
        {
            state.IsRunning = false;
            state.NeedsSave = true;
        }

        commands.Clear();
    }
}