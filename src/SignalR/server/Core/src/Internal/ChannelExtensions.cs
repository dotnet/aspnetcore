// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static class ChannelExtensions
{
    public static ValueTask RunAsync<TState>(this ChannelBasedSemaphore channelSemaphore, Func<TState, Task> callback, TState state)
    {
        if (channelSemaphore.AttemptWait())
        {
            _ = RunTask(callback, channelSemaphore, state);
            return ValueTask.CompletedTask;
        }

        return RunSlowAsync(channelSemaphore, callback, state);
    }

    private static async ValueTask RunSlowAsync<TState>(this ChannelBasedSemaphore channelSemaphore, Func<TState, Task> callback, TState state)
    {
        _ = await channelSemaphore.WaitAsync();
        _ = RunTask(callback, channelSemaphore, state);
    }

    static async Task RunTask<TState>(Func<TState, Task> callback, ChannelBasedSemaphore channelSemaphore, TState state)
    {
        try
        {
            await callback(state);
        }
        finally
        {
            await channelSemaphore.ReleaseAsync();
        }
    }
}
