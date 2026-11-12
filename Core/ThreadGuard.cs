// Core/ThreadGuard.cs
namespace AsciiFarm.Core;

/// <summary>
/// Блокиратор потока-владельца. AttachToCurrentThread() вызывается в начале потока,
/// после чего любой доступ из другого потока — исключение, а не гонка.
/// </summary>
public sealed class ThreadGuard
{
    private readonly string _name;
    private int _ownerThreadId;   // 0 = ещё не привязан
    private int _busy;            // 1 = данные прямо сейчас модифицируются

    public ThreadGuard(string name) => _name = name;

    public void AttachToCurrentThread() =>
        Interlocked.Exchange(ref _ownerThreadId, Environment.CurrentManagedThreadId);

    public void Validate()
    {
        int owner = Volatile.Read(ref _ownerThreadId);
        if (owner != 0 && owner != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException(
                $"«{_name}»: данные принадлежат потоку {owner}, " +
                $"доступ из потока {Environment.CurrentManagedThreadId} запрещён.");
    }

    /// <summary>Критическая секция изменения данных (запрещает повторный вход).</summary>
    public Scope Enter()
    {
        Validate();
        if (Interlocked.Exchange(ref _busy, 1) != 0)
            throw new InvalidOperationException($"«{_name}»: данные ещё модифицируются.");
        return new Scope(this);
    }

    private void Release() => Volatile.Write(ref _busy, 0);

    public readonly struct Scope : IDisposable
    {
        private readonly ThreadGuard _guard;
        internal Scope(ThreadGuard guard) => _guard = guard;
        public void Dispose() => _guard.Release();
    }
}