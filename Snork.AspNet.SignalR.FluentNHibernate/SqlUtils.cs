using System;
using System.Threading;
using System.Threading.Tasks;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class SqlUtils
    {
#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif
        public static async Task WrapForDeadlockAsync(CancellationToken cancellationToken, Func<Task> safeAction,
            TimeSpan retryInterval)
        {
            await WrapForDeadlockAsync(cancellationToken, async () =>
            {
                await safeAction();
                return true;
            }, retryInterval);
        }

#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif
        public static async Task<T> WrapForDeadlockAsync<T>(CancellationToken cancellationToken,
            Func<Task<T>> safeAction,
            TimeSpan retryInterval)
        {
            while (true)
                try
                {
                    return await safeAction();
                }
                catch (Exception ex)
                {
                    if (ex.Message.IndexOf("deadlock", StringComparison.InvariantCultureIgnoreCase) < 0)
                        throw;

                    cancellationToken.Wait(retryInterval);
                }
        }
#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif
        public static T WrapForDeadlock<T>(CancellationToken cancellationToken, Func<T> safeAction,
            TimeSpan retryInterval)
        {
            while (true)
                try
                {
                    return safeAction();
                }
                catch (Exception ex)
                {
                    if (ex.Message.IndexOf("deadlock", StringComparison.InvariantCultureIgnoreCase) < 0)
                        throw;

                    cancellationToken.Wait(retryInterval);
                }
        }
#if !DEBUG
[System.Diagnostics.DebuggerHidden]
#endif

        public static void WrapForDeadlock(CancellationToken cancellationToken, Action safeAction,
            TimeSpan retryInterval)
        {
            WrapForDeadlock(cancellationToken, () =>
            {
                safeAction();
                return true;
            }, retryInterval);
        }
    }
}