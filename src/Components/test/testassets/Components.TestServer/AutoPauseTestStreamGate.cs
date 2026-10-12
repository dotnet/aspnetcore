// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace TestServer;

public sealed class AutoPauseTestStreamGate
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _gates = new();
    private readonly ConcurrentDictionary<string, bool> _started = new();
    private readonly ConcurrentDictionary<string, bool> _completed = new();

    public Task WaitAsync(string token, CancellationToken cancellationToken)
    {
        var tcs = _gates.GetOrAdd(token, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        return tcs.Task.WaitAsync(cancellationToken);
    }

    public bool Release(string token)
    {
        var tcs = _gates.GetOrAdd(token, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        return tcs.TrySetResult();
    }

    public void MarkStarted(string token) => _started[token] = true;

    public bool IsStarted(string token) => _started.TryGetValue(token, out var v) && v;

    public void MarkCompleted(string token) => _completed[token] = true;

    public bool IsCompleted(string token) => _completed.TryGetValue(token, out var v) && v;
}

