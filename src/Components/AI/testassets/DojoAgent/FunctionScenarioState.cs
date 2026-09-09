// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace DojoAgent;

/// <summary>
/// Tracks server tool execution and gates results independently for each conversation.
/// </summary>
public sealed class FunctionScenarioState
{
    private readonly ConcurrentDictionary<string, InvocationState> _threads = new(StringComparer.Ordinal);

    /// <summary>Gets the number of actual server tool invocations in a conversation.</summary>
    /// <param name="threadId">The conversation's identifier.</param>
    /// <returns>The invocation count.</returns>
    public int GetInvocationCount(string threadId) => Volatile.Read(ref GetState(threadId).InvocationCount);

    /// <summary>Releases the pending informational tool result.</summary>
    /// <param name="threadId">The conversation's identifier.</param>
    public void ReleaseResult(string threadId) => GetState(threadId).ResultGate.TrySetResult();

    /// <summary>Removes a completed test conversation's state.</summary>
    /// <param name="threadId">The conversation's identifier.</param>
    public void Remove(string threadId) => _threads.TryRemove(threadId, out _);

    internal string Invoke(string threadId)
    {
        Interlocked.Increment(ref GetState(threadId).InvocationCount);

        return "sunny";
    }

    internal async Task<string> InvokeAsync(string threadId, CancellationToken cancellationToken)
    {
        var state = GetState(threadId);
        Interlocked.Increment(ref state.InvocationCount);
        try
        {
            await state.ResultGate.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            Interlocked.Exchange(ref state.ResultGate, new(TaskCreationOptions.RunContinuationsAsynchronously));
        }

        return "sunny";
    }

    private InvocationState GetState(string threadId)
    {
        ArgumentException.ThrowIfNullOrEmpty(threadId);

        return _threads.GetOrAdd(threadId, _ => new InvocationState());
    }

    private sealed class InvocationState
    {
        public int InvocationCount;

        public TaskCompletionSource ResultGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
