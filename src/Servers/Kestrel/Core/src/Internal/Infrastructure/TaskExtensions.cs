using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Threading.Tasks
{
    internal static class TaskExtensions
    {
        public static async Task<bool> WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            // This disposes the registration as soon as one of the tasks trigger
            using (cancellationToken.Register(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
            },
            tcs))
            {
                var resultTask = await Task.WhenAny(task, tcs.Task);
                if (resultTask == tcs.Task)
                {
                    // Operation cancelled
                    return false;
                }

                await task;
                return true;
            }
        }

        public static async Task<bool> ServerTimeoutAfter(this Task task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var delayTask = Task.Delay(timeout, cts.Token);

            var resultTask = await Task.WhenAny(task, delayTask);
            if (resultTask == delayTask)
            {
                // Operation cancelled
                return false;
            }
            else
            {
                // Cancel the timer task so that it does not fire
                cts.Cancel();
            }

            await task;
            return true;
        }
    }
}
