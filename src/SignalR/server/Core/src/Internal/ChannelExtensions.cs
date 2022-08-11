// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static class ChannelExtensions
{
    public static ValueTask RunAsync<TState>(this Channel<int> semaphoreSlim, Func<TState, Task> callback, TState state)
    {
        if (semaphoreSlim.Reader.TryRead(out _))
        {
            _ = RunTask(callback, semaphoreSlim, state);
            return ValueTask.CompletedTask;
        }

        return RunSlowAsync(semaphoreSlim, callback, state);
    }

    private static async ValueTask RunSlowAsync<TState>(this Channel<int> semaphoreSlim, Func<TState, Task> callback, TState state)
    {
        _ = await semaphoreSlim.Reader.ReadAsync();
        _ = RunTask(callback, semaphoreSlim, state);
    }

    static async Task RunTask<TState>(Func<TState, Task> callback, Channel<int> semaphoreSlim, TState state)
    {
        try
        {
            await callback(state);
        }
        finally
        {
            await semaphoreSlim.Writer.WriteAsync(1);
        }
    }
}
