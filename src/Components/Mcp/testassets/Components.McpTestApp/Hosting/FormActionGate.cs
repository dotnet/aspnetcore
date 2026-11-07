// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Components.McpTestApp.Hosting;

public sealed class FormActionGate
{
    private readonly ConcurrentDictionary<string, Gate> _gates = new();

    public async Task WaitAsync(string caseId, CancellationToken cancellationToken)
    {
        var gate = _gates.GetOrAdd(caseId, _ => new());
        using var registration = cancellationToken.Register(() => Interlocked.Exchange(ref gate.CancellationObserved, 1));
        Interlocked.Exchange(ref gate.Started, 1);
        await gate.Release.Task;
        cancellationToken.ThrowIfCancellationRequested();
    }

    public void Release(string caseId) => _gates.GetOrAdd(caseId, _ => new()).Release.TrySetResult();

    public object Read(string caseId)
    {
        var gate = _gates.GetOrAdd(caseId, _ => new());
        return new
        {
            Started = Volatile.Read(ref gate.Started) != 0,
            CancellationObserved = Volatile.Read(ref gate.CancellationObserved) != 0,
        };
    }

    private sealed class Gate
    {
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Started;
        public int CancellationObserved;
    }
}

public sealed class FormOperationCancellation(CancellationToken cancellationToken)
{
    public CancellationToken CancellationToken { get; } = cancellationToken;
}
