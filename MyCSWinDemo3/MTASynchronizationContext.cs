namespace CSWinWasapiDemo;

public sealed class MtaSynchronizationContext : SynchronizationContext
{
    private readonly TaskScheduler _scheduler;

    public MtaSynchronizationContext(TaskScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        Task.Factory.StartNew(
            () => d(state),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            _scheduler);
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        Task.Factory.StartNew(
            () => d(state),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            _scheduler).Wait();
    }
}