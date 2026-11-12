// Input/CommandChannel.cs
namespace AsciiFarm.Input;

public sealed class CommandChannel
{
    private readonly Queue<GameCommand> _queue = new();
    private readonly object _gate = new();
    private readonly int _capacity;
    private bool _closed;

    public CommandChannel(int capacity) => _capacity = capacity;

    public void Post(GameCommand command)
    {
        lock (_gate)
        {
            if (_closed || command == GameCommand.None) return;
            if (_queue.Count >= _capacity) _queue.Dequeue();
            _queue.Enqueue(command);
        }
    }

    public void DrainTo(List<GameCommand> target)
    {
        lock (_gate)
        {
            while (_queue.Count > 0) target.Add(_queue.Dequeue());
        }
    }

    public void Close()
    {
        lock (_gate) { _closed = true; _queue.Clear(); }
    }
}