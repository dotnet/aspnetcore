using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal static class SemaphoreSlimExtensions
    {
        public static async Task<Task> RunAsync<TState>(this SemaphoreSlim semaphoreSlim, Func<TState, Task> callback, TState state)
        {
            await semaphoreSlim.WaitAsync();
            return RunTask(callback, semaphoreSlim, state);

            static async Task RunTask(Func<TState, Task> callback, SemaphoreSlim semaphoreSlim, TState state)
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
}
