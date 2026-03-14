namespace Baksteen.Waves;

using System.Collections.Concurrent;

public sealed class MtaTaskScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> _tasks = new();
    private readonly List<Thread> _threads = new();

    public MtaTaskScheduler(int threadCount)
    {
        for (int i = 0; i < threadCount; i++)
        {
            var thread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new MtaSynchronizationContext(this));

                foreach (var task in _tasks.GetConsumingEnumerable())
                    TryExecuteTask(task);
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
            _threads.Add(thread);
        }
    }

    protected override IEnumerable<Task> GetScheduledTasks() => _tasks.ToArray();

    protected override void QueueTask(Task task) => _tasks.Add(task);

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

    public void Dispose()
    {
        _tasks.CompleteAdding();
        foreach (var t in _threads) t.Join();
    }
}
