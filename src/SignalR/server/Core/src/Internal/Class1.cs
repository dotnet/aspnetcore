using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public static class MyCoolAPI
    {
        public static async Task WaitToStartAsync<TState>(this SemaphoreSlim semaphoreSlim, Func<TState, Task> func, TState state)
        {
            await semaphoreSlim.WaitAsync();
            _ = RunTask(func, semaphoreSlim, state);

            static async Task RunTask(Func<TState, Task> func, SemaphoreSlim semaphoreSlim, TState state)
            {
                try
                {
                    await func(state);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
        }
    }
}
