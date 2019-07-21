// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.TestHost
{
    internal static class Utilities
    {
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        internal static Task<T> WithTimeout<T>(this Task<T> task) => task.WithTimeout(DefaultTimeout);

        internal static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));

            if (completedTask == task)
            {
                cts.Cancel();
                return await task;
            }
            else
            {
                throw new TimeoutException("The task has timed out.");
            }
        }
    }
}
