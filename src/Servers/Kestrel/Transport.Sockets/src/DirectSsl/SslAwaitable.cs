// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

/// <summary>
/// A reusable awaitable that avoids allocating TaskCompletionSource for each async operation.
/// Uses ManualResetValueTaskSourceCore for zero-allocation async patterns.
/// Thread-safe for concurrent TrySet* calls (first one wins).
/// </summary>
internal sealed class SslAwaitable<T> : IValueTaskSource<T>
{
    private ManualResetValueTaskSourceCore<T> _source;
    private readonly object _lock = new();
    private bool _isActive;
    private Exception? _exception;

    public SslAwaitable()
    {
        // RunContinuationsAsynchronously to avoid stack dives and deadlocks.
        // While this adds ThreadPool dispatch overhead, running inline caused crashes
        // under high concurrency (c=500) and didn't improve performance at c=100.
        _source.RunContinuationsAsynchronously = true;
    }

    /// <summary>
    /// Returns true if this awaitable is currently waiting for a result.
    /// </summary>
    public bool IsActive
    {
        get
        {
            lock (_lock)
            {
                return _isActive;
            }
        }
    }

    /// <summary>
    /// Prepares the awaitable for a new async wait and returns a ValueTask to await.
    /// </summary>
    public ValueTask<T> Reset()
    {
        lock (_lock)
        {
            if (_isActive)
            {
                throw new InvalidOperationException("SslAwaitable is already active");
            }

            _isActive = true;
            _exception = null;
            _source.Reset();
            return new ValueTask<T>(this, _source.Version);
        }
    }

    /// <summary>
    /// Completes the awaitable with a successful result.
    /// Thread-safe: first caller wins, subsequent calls return false.
    /// </summary>
    public bool TrySetResult(T result)
    {
        lock (_lock)
        {
            if (!_isActive)
            {
                return false;
            }

            _isActive = false;
            _source.SetResult(result);
            return true;
        }
    }

    /// <summary>
    /// Completes the awaitable with an exception.
    /// Thread-safe: first caller wins, subsequent calls return false.
    /// </summary>
    public bool TrySetException(Exception exception)
    {
        lock (_lock)
        {
            if (!_isActive)
            {
                return false;
            }

            _isActive = false;
            _exception = exception;
            _source.SetException(exception);
            return true;
        }
    }

    /// <summary>
    /// Cancels the awaitable.
    /// Thread-safe: first caller wins, subsequent calls return false.
    /// </summary>
    public bool TrySetCanceled()
    {
        lock (_lock)
        {
            if (!_isActive)
            {
                return false;
            }

            _isActive = false;
            var ex = new OperationCanceledException();
            _exception = ex;
            _source.SetException(ex);
            return true;
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
