// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;

internal class InMemoryTransportConnection : TransportConnection
{
    private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

    private readonly ILogger _logger;
    private bool _isClosed;
    private readonly TaskCompletionSource _waitForCloseTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public InMemoryTransportConnection(MemoryPool<byte> memoryPool, ILogger logger, PipeScheduler scheduler = null)
    {
        MemoryPool = memoryPool;
        _logger = logger;

        LocalEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
        RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

        var pair = DuplexPipe.CreateConnectionPair(new PipeOptions(memoryPool, readerScheduler: scheduler, useSynchronizationContext: false), new PipeOptions(memoryPool, writerScheduler: scheduler, useSynchronizationContext: false));
        Application = pair.Application;
        var wrapper = new ObservableDuplexPipe(pair.Transport);
        Transport = wrapper;
        WaitForReadTask = wrapper.WaitForReadTask;

        ConnectionClosed = _connectionClosedTokenSource.Token;
    }

    public PipeWriter Input => Application.Output;

    public PipeReader Output => Application.Input;

    public Task WaitForReadTask { get; }

    public override MemoryPool<byte> MemoryPool { get; }

    public ConnectionAbortedException AbortReason { get; private set; }

    public Task WaitForCloseTask => _waitForCloseTcs.Task;

    public override void Abort(ConnectionAbortedException abortReason)
    {
        _logger.LogDebug(@"Connection id ""{ConnectionId}"" closing because: ""{Message}""", ConnectionId, abortReason?.Message);

        Input.Complete(abortReason);

        OnClosed();

        AbortReason = abortReason;
    }

    public void OnClosed()
    {
        if (Interlocked.CompareExchange(ref _isClosed, true, false) == true)
        {
            return;
        }

        ThreadPool.UnsafeQueueUserWorkItem(state =>
        {
            state._connectionClosedTokenSource.Cancel();

            state._waitForCloseTcs.TrySetResult();
        },
        this,
        preferLocal: false);
    }

    public override async ValueTask DisposeAsync()
    {
        Transport.Input.Complete();
        Transport.Output.Complete();

        await _waitForCloseTcs.Task;

        _connectionClosedTokenSource.Dispose();
    }

    // This piece of code allows us to wait until the PipeReader has been awaited on.
    // We need to wrap lots of layers (including the ValueTask) to gain visiblity into when
    // the machinery for the await happens
    private class ObservableDuplexPipe : IDuplexPipe
    {
        private readonly ObservablePipeReader _reader;

        public ObservableDuplexPipe(IDuplexPipe duplexPipe)
        {
            _reader = new ObservablePipeReader(duplexPipe.Input);

            Input = _reader;
            Output = duplexPipe.Output;

        }

        public Task WaitForReadTask => _reader.WaitForReadTask;

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        private class ObservablePipeReader : PipeReader
        {
            private readonly PipeReader _reader;
            private readonly TaskCompletionSource _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task WaitForReadTask => _tcs.Task;

            public ObservablePipeReader(PipeReader reader)
            {
                _reader = reader;
            }

            public override void AdvanceTo(SequencePosition consumed)
            {
                _reader.AdvanceTo(consumed);
            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {
                _reader.AdvanceTo(consumed, examined);
            }

            public override void CancelPendingRead()
            {
                _reader.CancelPendingRead();
            }

            public override void Complete(Exception exception = null)
            {
                _reader.Complete(exception);
            }

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                var task = _reader.ReadAsync(cancellationToken);

                if (_tcs.Task.IsCompleted)
                {
                    return task;
                }

                return new ValueTask<ReadResult>(new ObservableValueTask<ReadResult>(task, _tcs), 0);
            }

            public override bool TryRead(out ReadResult result)
            {
                return _reader.TryRead(out result);
            }

            private class ObservableValueTask<T> : IValueTaskSource<T>
            {
                private readonly ValueTask<T> _task;
                private readonly TaskCompletionSource _tcs;

                public ObservableValueTask(ValueTask<T> task, TaskCompletionSource tcs)
                {
                    _task = task;
                    _tcs = tcs;
                }

                public T GetResult(short token)
                {
                    return _task.GetAwaiter().GetResult();
                }

                public ValueTaskSourceStatus GetStatus(short token)
                {
                    if (_task.IsCanceled)
                    {
                        return ValueTaskSourceStatus.Canceled;
                    }
                    if (_task.IsFaulted)
                    {
                        return ValueTaskSourceStatus.Faulted;
                    }
                    if (_task.IsCompleted)
                    {
                        return ValueTaskSourceStatus.Succeeded;
                    }
                    return ValueTaskSourceStatus.Pending;
                }

                public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
                {
                    _task.GetAwaiter().UnsafeOnCompleted(() => continuation(state));

                    _tcs.TrySetResult();
                }
            }
        }
    }
}
