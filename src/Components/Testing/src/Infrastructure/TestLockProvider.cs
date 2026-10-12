// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

// Singleton service that provides test-controlled async gates.
// A service override calls WaitOn("key") to get a Task that blocks until the
// test releases it via POST /_test/lock/release?key=... through the YARP proxy.
//
// Typical lock key convention: "{sessionId}:{lockName}" where sessionId
// comes from TestSessionContext and lockName is chosen by the test class.
//
// Always registered by the hosting startup. No-op if never called.
/// <summary>
/// Singleton service that provides test-controlled async gates.
/// A service override calls <see cref="WaitOn"/> to get a <see cref="Task"/> that blocks until the
/// test releases it via <c>POST /_test/lock/release?key=...</c> through the YARP proxy.
/// </summary>
/// <remarks>
/// Typical lock key convention: <c>{sessionId}:{lockName}</c> where sessionId
/// comes from <see cref="TestSessionContext"/> and lockName is chosen by the test class.
/// Always registered by the hosting startup. No-op if never called.
/// </remarks>
public class TestLockProvider
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _locks = new();

    /// <summary>
    /// Returns a <see cref="Task"/> that completes when <see cref="Release"/> is called with the same key.
    /// Calling with the same key multiple times returns the same <see cref="Task"/>.
    /// </summary>
    /// <param name="key">The lock key, typically <c>{sessionId}:{lockName}</c>.</param>
    public Task WaitOn(string key)
    {
        var tcs = _locks.GetOrAdd(key,
            _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        return tcs.Task;
    }

    /// <summary>
    /// Completes the <see cref="Task"/> returned by <see cref="WaitOn"/>.
    /// If <see cref="WaitOn"/> hasn't been called yet, pre-creates a completed TCS so
    /// the subsequent call returns immediately (race-safe).
    /// </summary>
    /// <param name="key">The lock key to release.</param>
    /// <returns><c>true</c> if the TCS was newly completed by this call.</returns>
    public bool Release(string key)
    {
        var tcs = _locks.GetOrAdd(key,
            _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        return tcs.TrySetResult();
    }
}
