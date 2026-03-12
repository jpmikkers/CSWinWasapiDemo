namespace CSWinWasapiDemo;

public static class WaitHandleExtensions
{
#if USELESS
    public static Task WaitOneAsync(this WaitHandle handle, CancellationToken token = default)
    {
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var rwh = ThreadPool.RegisterWaitForSingleObject(
            handle,
            (state, timedOut) => ((TaskCompletionSource<object?>)state!).TrySetResult(null),
            tcs,
            Timeout.Infinite,
            executeOnlyOnce: true);

        if (token != default)
        {
            token.Register(() =>
            {
                rwh.Unregister(null);
                tcs.TrySetCanceled(token);
            });
        }

        return tcs.Task;
    }

    public static Task WaitAsync(this ManualResetEvent evt, TaskScheduler scheduler)
    {
        var tcs = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ThreadPool.QueueUserWorkItem(_ =>
        {
            evt.WaitOne();
            Task.Factory.StartNew(
                () => tcs.TrySetResult(null),
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                scheduler);
        });

        return tcs.Task;
    }
#endif
}
