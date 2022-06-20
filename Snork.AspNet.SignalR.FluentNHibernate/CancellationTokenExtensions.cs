using System;
using System.Diagnostics;
using System.Threading;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal static class CancellationTokenExtensions
    {
        /// <summary>
        ///     Returns a class that contains a <see cref="T:System.Threading.EventWaitHandle" /> that is set, when
        ///     the given <paramref name="cancellationToken" /> is canceled. This method is based
        ///     on cancellation token registration and avoids using the
        ///     <see cref="P:System.Threading.CancellationToken.WaitHandle" />
        ///     property as it may lead to high CPU issues.
        /// </summary>
        public static CancellationEvent GetCancellationEvent(
            this CancellationToken cancellationToken)
        {
            return new CancellationEvent(cancellationToken);
        }


        /// <summary>
        ///     Performs a wait until the specified <paramref name="timeout" /> is elapsed or the
        ///     given cancellation token is canceled. The wait is performed on a dedicated event
        ///     wait handle to avoid using the <see cref="P:System.Threading.CancellationToken.WaitHandle" /> property
        ///     that may lead to high CPU issues.
        /// </summary>
        public static bool Wait(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            using (var cancellationEvent = cancellationToken.GetCancellationEvent())
            {
                var stopwatch = Stopwatch.StartNew();
                var flag = cancellationEvent.WaitHandle.WaitOne(timeout);
                stopwatch.Stop();
                var timeSpan1 = TimeSpan.FromMilliseconds(1000.0);
                var timeSpan2 = TimeSpan.FromMilliseconds(500.0);
                var timeout1 = TimeSpan.FromSeconds(1.0);
                if (!cancellationToken.IsCancellationRequested && timeout >= timeSpan1 && stopwatch.Elapsed < timeSpan2)
                    Thread.Sleep(timeout1);

                return flag;
            }
        }

        internal class CancellationEvent : IDisposable
        {
            private static readonly Action<object> SetEventCallback = SetEvent;
            private readonly ManualResetEvent _mre;
            private CancellationTokenRegistration _registration;

            public CancellationEvent(CancellationToken cancellationToken)
            {
                _mre = new ManualResetEvent(false);
                _registration = cancellationToken.Register(SetEventCallback, _mre);
            }

            public EventWaitHandle WaitHandle => _mre;

            public void Dispose()
            {
                _registration.Dispose();
                _mre.Dispose();
            }

            private static void SetEvent(object state)
            {
                try
                {
                    ((EventWaitHandle) state).Set();
                }
                catch (ObjectDisposedException ex)
                {
                }
            }
        }
    }
}