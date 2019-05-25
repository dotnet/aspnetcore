// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    internal class InMemoryTransportConnection : TransportConnection
    {
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

        private readonly ILogger _logger;
        private bool _isClosed;

        public InMemoryTransportConnection(MemoryPool<byte> memoryPool, ILogger logger, PipeScheduler scheduler = null)
        {
            MemoryPool = memoryPool;
            _logger = logger;

            LocalEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

            var pair = DuplexPipe.CreateConnectionPair(new PipeOptions(memoryPool, readerScheduler: scheduler), new PipeOptions(memoryPool, writerScheduler: scheduler));
            Application = pair.Application;
            var wrapper = new ObservableDuplexPipe(pair.Transport);
            Transport = wrapper;
            WaitForReadTask = wrapper.WaitForReadTask;

            ConnectionClosed = _connectionClosedTokenSource.Token;
        }

        public Task WaitForReadTask { get; }

        public override MemoryPool<byte> MemoryPool { get; }

        public ConnectionAbortedException AbortReason { get; private set; }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            _logger.LogDebug(@"Connection id ""{ConnectionId}"" closing because: ""{Message}""", ConnectionId, abortReason?.Message);

            Input.Complete(abortReason);

            AbortReason = abortReason;
        }

        public void OnClosed()
        {
            if (_isClosed)
            {
                return;
            }

            _isClosed = true;

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                var self = (InMemoryTransportConnection)state;
                self._connectionClosedTokenSource.Cancel();
            }, this);
        }

        public override ValueTask DisposeAsync()
        {
            _connectionClosedTokenSource.Dispose();

            Transport.Input.Complete();
            Transport.Output.Complete();

            return base.DisposeAsync();
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
                private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

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

                public override void OnWriterCompleted(Action<Exception, object> callback, object state)
                {
                    _reader.OnWriterCompleted(callback, state);
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
                    private readonly TaskCompletionSource<object> _tcs;

                    public ObservableValueTask(ValueTask<T> task, TaskCompletionSource<object> tcs)
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

                        _tcs.TrySetResult(null);
                    }
                }
            }
        }
    }
}
