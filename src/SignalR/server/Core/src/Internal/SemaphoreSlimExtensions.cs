// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
