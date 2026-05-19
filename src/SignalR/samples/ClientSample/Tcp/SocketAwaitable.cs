// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientSample;

public class SocketAwaitable : ICriticalNotifyCompletion
{
    private static readonly Action _callbackCompleted = () => { };

    private readonly PipeScheduler _ioScheduler;

    private Action _callback;
    private int _bytesTransferred;
    private SocketError _error;

    public SocketAwaitable(PipeScheduler ioScheduler)
    {
        _ioScheduler = ioScheduler;
    }

    public SocketAwaitable GetAwaiter() => this;
    public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

    public int GetResult()
    {
        Debug.Assert(ReferenceEquals(_callback, _callbackCompleted));

        _callback = null;

        if (_error != SocketError.Success)
        {
            throw new SocketException((int)_error);
        }

        return _bytesTransferred;
    }

    public void OnCompleted(Action continuation)
    {
        if (ReferenceEquals(_callback, _callbackCompleted) ||
            ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
        {
            Task.Run(continuation);
        }
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        OnCompleted(continuation);
    }

    public void Complete(int bytesTransferred, SocketError socketError)
    {
        _error = socketError;
        _bytesTransferred = bytesTransferred;
        var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);

        if (continuation != null)
        {
            _ioScheduler.Schedule(state => ((Action)state)(), continuation);
        }
    }
}
