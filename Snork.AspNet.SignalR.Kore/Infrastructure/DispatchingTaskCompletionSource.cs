using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Snork.AspNet.SignalR.Kore.Infrastructure
{
    internal class DispatchingTaskCompletionSource<TResult>
    {
        private readonly TaskCompletionSource<TResult> _taskCompletionSource = new TaskCompletionSource<TResult>();

        public Task<TResult> Task
        {
            get { return _taskCompletionSource.Task; }
        }

        public void SetCanceled()
        {
            TaskAsyncHelper.Dispatch(() => _taskCompletionSource.SetCanceled());
        }

        public void SetException(Exception exception)
        {
            TaskAsyncHelper.Dispatch(() => _taskCompletionSource.SetException(exception));
        }

        public void SetException(IEnumerable<Exception> exceptions)
        {
            TaskAsyncHelper.Dispatch(() => _taskCompletionSource.SetException(exceptions));
        }

        public void SetResult(TResult result)
        {
            TaskAsyncHelper.Dispatch(() => _taskCompletionSource.SetResult(result));
        }

        public void TrySetCanceled()
        {
            TaskAsyncHelper.Dispatch(() => _taskCompletionSource.TrySetCanceled());
        }

        public void TrySetException(Exception exception)
        {
            TaskAsyncHelper.Dispatch(() => _taskCompletionSource.TrySetException(exception));
        }

        public void TrySetException(IEnumerable<Exception> exceptions)
        {
            TaskAsyncHelper.Dispatch(() => _taskCompletionSource.TrySetException(exceptions));
        }

        public void SetUnwrappedException(Exception e)
        {
            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                SetException(aggregateException.InnerExceptions);
            }
            else
            {
                SetException(e);
            }
        }

        public void TrySetUnwrappedException(Exception e)
        {
            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                TrySetException(aggregateException.InnerExceptions);
            }
            else
            {
                TrySetException(e);
            }
        }

        public void TrySetResult(TResult result)
        {
            TaskAsyncHelper.Dispatch(() => _taskCompletionSource.TrySetResult(result));
        }
    }
}