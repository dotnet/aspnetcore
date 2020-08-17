// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal static class SemaphoreSlimExtensions
    {
        public static Task RunAsync<TState>(this SemaphoreSlim semaphoreSlim, Func<TState, Task> callback, TState state)
        {
            if (semaphoreSlim.Wait(0))
            {
                _ = RunTask(callback, semaphoreSlim, state);
                return Task.CompletedTask;
            }

            return RunSlowAsync(semaphoreSlim, callback, state);
        }

        private static async Task<Task> RunSlowAsync<TState>(this SemaphoreSlim semaphoreSlim, Func<TState, Task> callback, TState state)
        {
            await semaphoreSlim.WaitAsync();
            return RunTask(callback, semaphoreSlim, state);
        }

        static async Task RunTask<TState>(Func<TState, Task> callback, SemaphoreSlim semaphoreSlim, TState state)
        {
            try
            {
                await callback(state);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}
