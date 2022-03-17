// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.Extensions.Internal;

internal class SingleThreadedSynchronizationContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new BlockingCollection<(SendOrPostCallback Callback, object? State)>();

    public override void Send(SendOrPostCallback d, object? state) // Sync operations
    {
        throw new NotSupportedException($"{nameof(SingleThreadedSynchronizationContext)} does not support synchronous operations.");
    }

    public override void Post(SendOrPostCallback d, object? state) // Async operations
    {
        _queue.Add((d, state));
    }

    public static void Run(Action action)
    {
        var previous = Current;
        var context = new SingleThreadedSynchronizationContext();
        SetSynchronizationContext(context);
        try
        {
            action();

            while (context._queue.TryTake(out var item))
            {
                item.Callback(item.State);
            }
        }
        finally
        {
            context._queue.CompleteAdding();
            SetSynchronizationContext(previous);
        }
    }
}
