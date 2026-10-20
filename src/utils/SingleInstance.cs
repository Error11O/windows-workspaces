namespace workspaces.Utils;

public sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activation;

    public SingleInstance(string name)
    {
        _activation = new EventWaitHandle(false, EventResetMode.AutoReset, name + ".Activate");
        _mutex = new Mutex(initiallyOwned: true, name, out var createdNew);
        IsOwner = createdNew;
    }

    public bool IsOwner { get; }

    public void RequestActivation() => _activation.Set();

    public IDisposable ListenForActivation(Action activate)
    {
        if (!IsOwner) throw new InvalidOperationException("Only the running instance can listen for activation.");
        return new ActivationRegistration(ThreadPool.RegisterWaitForSingleObject(_activation,
            (_, _) => activate(), null, Timeout.Infinite, executeOnlyOnce: false));
    }

    private sealed class ActivationRegistration(RegisteredWaitHandle registration) : IDisposable
    {
        public void Dispose() => registration.Unregister(null);
    }

    public void Dispose()
    {
        if (IsOwner)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
        _activation.Dispose();
    }
}
