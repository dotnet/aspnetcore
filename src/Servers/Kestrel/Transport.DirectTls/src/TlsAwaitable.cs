// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// A reusable awaitable that avoids allocating a <see cref="TaskCompletionSource"/> for each
/// async read/write. Wraps <see cref="ManualResetValueTaskSourceCore{TResult}"/> for the
/// continuation/result plumbing and adds a single lock-free "is a wait pending" gate.
/// </summary>
/// <remarks>
/// Exactly two threads touch an instance: the ThreadPool loop that arms it (<see cref="Reset"/>)
/// and the epoll pump thread that completes it (<c>TrySet*</c>). They never call
/// <see cref="ManualResetValueTaskSourceCore{TResult}.Reset"/> and <c>Set*</c> concurrently
/// because the loop cannot advance past <c>await</c> (and therefore cannot call
/// <see cref="Reset"/> again) until a completion has run, so a plain
/// <see cref="Interlocked.CompareExchange(ref int,int,int)"/> on the state is enough — no lock.
/// The <see cref="Volatile"/> write in <see cref="Reset"/> also publishes the buffers/flags the
/// caller set beforehand to the pump thread (release/acquire), which is the memory barrier the
/// previous <c>lock</c> provided.
/// </remarks>
internal sealed class TlsAwaitable<T> : IValueTaskSource<T>
{
    private const int Idle = 0;
    private const int Active = 1;

    private ManualResetValueTaskSourceCore<T> _source;
    private int _state;

    public TlsAwaitable()
    {
        // RunContinuationsAsynchronously to avoid stack dives and deadlocks.
        // While this adds ThreadPool dispatch overhead, running inline caused crashes
        // under high concurrency (c=500) and didn't improve performance at c=100.
        _source.RunContinuationsAsynchronously = true;
    }

    /// <summary>
    /// Returns true if this awaitable is currently waiting for a result.
    /// </summary>
    public bool IsActive => Volatile.Read(ref _state) == Active;

    /// <summary>
    /// Prepares the awaitable for a new async wait and returns a ValueTask to await.
    /// Called only by the consumer loop, and only after the previous wait has completed.
    /// </summary>
    public ValueTask<T> Reset()
    {
        if (Volatile.Read(ref _state) == Active)
        {
            throw new InvalidOperationException("TlsAwaitable is already active");
        }

        _source.Reset();

        // Release fence: publishes _source.Reset() and any read/write buffers and flags the
        // caller assigned before calling Reset(), so the pump thread that observes Active
        // (via IsActive's acquiring Volatile.Read) also sees those writes.
        Volatile.Write(ref _state, Active);
        return new ValueTask<T>(this, _source.Version);
    }

    /// <summary>
    /// Completes the awaitable with a successful result.
    /// Thread-safe: first caller wins, subsequent calls return false.
    /// </summary>
    public bool TrySetResult(T result)
    {
        if (Interlocked.CompareExchange(ref _state, Idle, Active) != Active)
        {
            return false;
        }

        _source.SetResult(result);
        return true;
    }

    /// <summary>
    /// Completes the awaitable with an exception.
    /// Thread-safe: first caller wins, subsequent calls return false.
    /// </summary>
    public bool TrySetException(Exception exception)
    {
        if (Interlocked.CompareExchange(ref _state, Idle, Active) != Active)
        {
            return false;
        }

        _source.SetException(exception);
        return true;
    }

    /// <summary>
    /// Cancels the awaitable.
    /// Thread-safe: first caller wins, subsequent calls return false.
    /// </summary>
    public bool TrySetCanceled()
    {
        if (Interlocked.CompareExchange(ref _state, Idle, Active) != Active)
        {
            return false;
        }

        _source.SetException(new OperationCanceledException());
        return true;
    }

    // IValueTaskSource<T> implementation
    public T GetResult(short token)
    {
        // We don't validate the token since we control all usage
        return _source.GetResult(token);
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _source.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _source.OnCompleted(continuation, state, token, flags);
    }
}
