// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Internal;

public class SyncPoint
{
    private readonly TaskCompletionSource _atSyncPoint = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _continueFromSyncPoint = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Waits for the code-under-test to reach <see cref="WaitToContinue"/>.
    /// </summary>
    /// <returns></returns>
    public Task WaitForSyncPoint() => _atSyncPoint.Task;

    /// <summary>
    /// Releases the code-under-test to continue past where it waited for <see cref="WaitToContinue"/>.
    /// </summary>
    public void Continue() => _continueFromSyncPoint.TrySetResult();

    /// <summary>
    /// Used by the code-under-test to wait for the test code to sync up.
    /// </summary>
    /// <remarks>
    /// This code will unblock <see cref="WaitForSyncPoint"/> and then block waiting for <see cref="Continue"/> to be called.
    /// </remarks>
    /// <returns></returns>
    public Task WaitToContinue()
    {
        _atSyncPoint.TrySetResult();
        return _continueFromSyncPoint.Task;
    }

    public static Func<Task> Create(out SyncPoint syncPoint)
    {
        var handler = Create(1, out var syncPoints);
        syncPoint = syncPoints[0];
        return handler;
    }

    /// <summary>
    /// Creates a re-entrant function that waits for sync points in sequence.
    /// </summary>
    /// <param name="count">The number of sync points to expect</param>
    /// <param name="syncPoints">The <see cref="SyncPoint"/> objects that can be used to coordinate the sync point</param>
    /// <returns></returns>
    public static Func<Task> Create(int count, out SyncPoint[] syncPoints)
    {
        // Need to use a local so the closure can capture it. You can't use out vars in a closure.
        var localSyncPoints = new SyncPoint[count];
        for (var i = 0; i < count; i += 1)
        {
            localSyncPoints[i] = new SyncPoint();
        }

        syncPoints = localSyncPoints;

        var counter = 0;
        return () =>
        {
            if (counter >= localSyncPoints.Length)
            {
                return Task.CompletedTask;
            }
            else
            {
                var syncPoint = localSyncPoints[counter];

                counter += 1;
                return syncPoint.WaitToContinue();
            }
        };
    }
}
