// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices
{
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    internal static class TaskExtensions
    {
        [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
        public static Task OrThrowOnCancellation(this Task task, CancellationToken cancellationToken)
        {
            return task.IsCompleted
                ? task // If the task is already completed, no need to wrap it in a further layer of task
                : task.ContinueWith(
                    _ => {}, // If the task completes, allow execution to continue
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
        public static Task<T> OrThrowOnCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            return task.IsCompleted
                ? task // If the task is already completed, no need to wrap it in a further layer of task
                : task.ContinueWith(
                    t => t.Result, // If the task completes, pass through its result
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
    }
}
