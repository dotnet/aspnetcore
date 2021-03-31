// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal static class StreamUtilities
    {
        /// <summary>
        /// The segment size for buffering the response body in bytes. The default is set to 80 KB (81920 Bytes) to avoid allocations on the LOH.
        /// </summary>
        // Internal for testing
        internal static int BodySegmentSize { get; set; } = 81920;

        internal static IAsyncResult ToIAsyncResult(Task task, AsyncCallback? callback, object? state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception!.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(0);
                }

                callback?.Invoke(tcs.Task);
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}
