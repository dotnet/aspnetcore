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
/// Three participants touch an instance, coordinated entirely through the single <c>_state</c>
/// word via <see cref="Interlocked"/>: the ThreadPool loop that arms it (<see cref="Reset"/>), the
/// epoll pump thread that completes a pending wait (<c>TrySet*</c>), and connection disposal, which
/// permanently <see cref="Cancel">cancels</see> it. The arming loop cannot advance past <c>await</c>
/// (and therefore cannot call <see cref="Reset"/> again) until a completion has run, so the pump and
/// the loop never touch <see cref="ManualResetValueTaskSourceCore{TResult}.Reset"/>/<c>Set*</c>
/// concurrently. <see cref="Cancel"/> may run at any time, but because it and every other transition
/// are <see cref="Interlocked"/> operations on <c>_state</c>, they are totally ordered on one word:
/// exactly one of them completes the underlying source for a given version, so arming and cancelling
/// can never both "miss" (the lost-cancel race a separate flag would leave open).
/// The state transition in <see cref="Reset"/> also publishes the buffers/flags the caller set
/// beforehand to the pump thread (release/acquire), which is the memory barrier the previous
/// <c>lock</c> provided.
/// </remarks>
internal sealed class TlsAwaitable<T> : IValueTaskSource<T>
{
    private const int Idle = 0;
    private const int Active = 1;

    // Sticky terminal state set by Cancel() on connection disposal. Once here the awaitable never
    // arms again: Reset() hands back an already-cancelled result instead of parking a wait that the
    // pump can no longer complete (its fd is being removed from epoll).
    private const int Canceled = 2;

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
    /// Prepares the awaitable for a new async wait and returns a <see cref="ValueTask{T}"/> to await.
    /// Called only by the consumer loop, and only after the previous wait has completed. If the
    /// awaitable has been <see cref="Cancel">canceled</see> (the connection is being disposed), this
    /// returns an already-canceled result instead of arming a wait the pump can no longer complete.
    /// </summary>
    public ValueTask<T> Reset()
    {
        if (Volatile.Read(ref _state) == Active)
        {
            throw new InvalidOperationException("TlsAwaitable is already active");
        }

        _source.Reset();

        // Idle -> Active arms the wait. This Interlocked full fence publishes _source.Reset() and any
        // buffers/flags the caller set beforehand to the pump thread (which observes Active via IsActive's
        // acquiring read). If the state is instead the sticky Canceled - because Cancel() already ran, or
        // wins the race with this CAS - the CAS is a no-op and we hand back an already-canceled ValueTask,
        // so the consumer loop unwinds rather than parking on an epoll event that will never arrive once
        // the fd is unregistered during disposal.
        if (Interlocked.CompareExchange(ref _state, Active, Idle) == Canceled)
        {
            _source.SetException(new OperationCanceledException());
        }

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
    /// Permanently cancels the awaitable. Any in-flight wait is completed with
    /// <see cref="OperationCanceledException"/>, and every subsequent <see cref="Reset"/> returns an
    /// already-cancelled result. Called once, from connection disposal, and coordinates with
    /// <see cref="Reset"/> and the pump's <c>TrySet*</c> through the single <c>_state</c> word so a
    /// wait armed concurrently with disposal is always completed rather than left parked forever.
    /// </summary>
    public void Cancel()
    {
        // Exchange to the sticky Canceled state. If a wait was pending (Active), we won the race against
        // the pump's TrySet* (whose Active-conditioned CAS now fails), so we are the sole completer of the
        // source. If it was Idle, the loop is between operations; its next Reset() observes Canceled and
        // returns a cancelled result. Either way the loop cannot re-arm and park after this point.
        if (Interlocked.Exchange(ref _state, Canceled) == Active)
        {
            _source.SetException(new OperationCanceledException());
        }
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
