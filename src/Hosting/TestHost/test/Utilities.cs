// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.TestHost
{
    internal static class Utilities
    {
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        internal static ConfiguredTaskAwaitable<T> WithTimeout<T>(this Task<T> task) => task.WithTimeout(DefaultTimeout).ConfigureAwait(false);

        private static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);

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

        internal static ConfiguredTaskAwaitable WithTimeout(this Task task) => task.WithTimeout(DefaultTimeout).ConfigureAwait(false);

        private static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);

            if (completedTask == task)
            {
                cts.Cancel();
                await task;
            }
            else
            {
                throw new TimeoutException("The task has timed out.");
            }
        }
    }
}
