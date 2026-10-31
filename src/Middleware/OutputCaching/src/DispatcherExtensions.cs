// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class WorkDispatcher<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Task<TValue?>> _workers = new();

    public async Task<TValue?> ScheduleAsync(TKey key, Func<TKey, Task<TValue?>> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);

        while (true)
        {
            if (_workers.TryGetValue(key, out var task))
            {
                return await task;
            }

            // This is the task that we'll return to all waiters. We'll complete it when the factory is complete
            var tcs = new TaskCompletionSource<TValue?>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (_workers.TryAdd(key, tcs.Task))
            {
                try
                {
                    var value = await valueFactory(key);
                    tcs.TrySetResult(value);
                    return await tcs.Task;
                }
                catch (Exception ex)
                {
                    // Make sure all waiters see the exception
                    tcs.SetException(ex);

                    throw;
                }
                finally
                {
                    // We remove the entry if the factory failed so it's not a permanent failure
                    // and future gets can retry (this could be a pluggable policy)
                    _workers.TryRemove(key, out _);
                }
            }
        }
    }

    public async Task<TValue?> ScheduleAsync<TState>(TKey key, TState state, Func<TKey, TState, Task<TValue?>> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);

        while (true)
        {
            if (_workers.TryGetValue(key, out var task))
            {
                return await task;
            }

            // This is the task that we'll return to all waiters. We'll complete it when the factory is complete
            var tcs = new TaskCompletionSource<TValue?>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (_workers.TryAdd(key, tcs.Task))
            {
                try
                {
                    var value = await valueFactory(key, state);
                    tcs.TrySetResult(value);
                    return await tcs.Task;
                }
                catch (Exception ex)
                {
                    // Make sure all waiters see the exception
                    tcs.SetException(ex);

                    throw;
                }
                finally
                {
                    // We remove the entry if the factory failed so it's not a permanent failure
                    // and future gets can retry (this could be a pluggable policy)
                    _workers.TryRemove(key, out _);
                }
            }
        }
    }
}
