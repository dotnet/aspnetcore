// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal abstract class AsyncIOOperation : IValueTaskSource<int>, IValueTaskSource
{
    private static readonly Action<object?> CallbackCompleted = _ => { Debug.Assert(false, "Should not be invoked"); };

    private Action<object?>? _continuation;
    private object? _state;
    private int _result;

    private Exception? _exception;

    public ValueTaskSourceStatus GetStatus(short token)
    {
        if (ReferenceEquals(Volatile.Read(ref _continuation), null))
        {
            return ValueTaskSourceStatus.Pending;
        }

        return _exception != null ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Faulted;
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        if (_state != null)
        {
            ThrowMultipleContinuations();
        }

        _state = state;

        var previousContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);

        if (previousContinuation != null)
        {
            if (!ReferenceEquals(previousContinuation, CallbackCompleted))
            {
                ThrowMultipleContinuations();
            }

            new AsyncContinuation(continuation, state).Invoke();
        }
    }

    private static void ThrowMultipleContinuations()
    {
        throw new InvalidOperationException("Multiple awaiters are not allowed");
    }

    void IValueTaskSource.GetResult(short token)
    {
        var exception = _exception;

        ResetOperation();

        if (exception != null)
        {
            throw exception;
        }
    }

    public int GetResult(short token)
    {
        var exception = _exception;
        var result = _result;

        ResetOperation();

        if (exception != null)
        {
            throw exception;
        }

        return result;
    }

    public AsyncContinuation? Invoke()
    {
        if (InvokeOperation(out var hr, out var bytes))
        {
            return Complete(hr, bytes);
        }
        return null;
    }

    protected abstract bool InvokeOperation(out int hr, out int bytes);

    public AsyncContinuation Complete(int hr, int bytes)
    {
        if (hr != NativeMethods.ERROR_OPERATION_ABORTED)
        {
            _result = bytes;
            if (hr != NativeMethods.HR_OK && !IsSuccessfulResult(hr))
            {
                // Treat all errors as the client disconnect
                _exception = new ConnectionResetException("The client has disconnected", Marshal.GetExceptionForHR(hr)!);
            }
        }
        else
        {
            _result = -1;
            _exception = null;
        }

        AsyncContinuation asyncContinuation = default;
        var continuation = Interlocked.CompareExchange(ref _continuation, CallbackCompleted, null);
        if (continuation != null)
        {
            asyncContinuation = new AsyncContinuation(continuation, _state);
        }

        FreeOperationResources(hr, bytes);

        return asyncContinuation;
    }

    protected virtual bool IsSuccessfulResult(int hr) => false;

    public virtual void FreeOperationResources(int hr, int bytes) { }

    protected virtual void ResetOperation()
    {
        _exception = null;
        _result = int.MinValue;
        _state = null;
        _continuation = null;
    }

    public bool InUse() => _continuation is not null;

    public readonly struct AsyncContinuation
    {
        public Action<object?> Continuation { get; }
        public object? State { get; }

        public AsyncContinuation(Action<object?> continuation, object? state)
        {
            Continuation = continuation;
            State = state;
        }

        public void Invoke()
        {
            if (Continuation != null)
            {
                ThreadPool.UnsafeQueueUserWorkItem(Continuation, State, preferLocal: false);
            }
        }
    }
}
