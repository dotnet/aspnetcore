// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO
{
    internal partial class AsyncIOEngine : IAsyncIOEngine
    {
        private readonly IISHttpContext _context;
        private readonly NativeSafeHandle _handler;

        private bool _stopped;

        private AsyncIOOperation? _nextOperation;
        private AsyncIOOperation? _runningOperation;

        private AsyncReadOperation? _cachedAsyncReadOperation;
        private AsyncWriteOperation? _cachedAsyncWriteOperation;
        private AsyncFlushOperation? _cachedAsyncFlushOperation;

        public AsyncIOEngine(IISHttpContext context, NativeSafeHandle handler)
        {
            _context = context;
            _handler = handler;
        }

        public ValueTask<int> ReadAsync(Memory<byte> memory)
        {
            var read = GetReadOperation();
            read.Initialize(_handler, memory);
            Run(read);
            return new ValueTask<int>(read, 0);
        }

        public ValueTask<int> WriteAsync(ReadOnlySequence<byte> data)
        {
            var write = GetWriteOperation();
            write.Initialize(_handler, data);
            Run(write);
            return new ValueTask<int>(write, 0);
        }

        private void Run(AsyncIOOperation ioOperation)
        {
            lock (_context._contextLock)
            {
                if (_stopped)
                {
                    // Abort all operation after IO was stopped
                    ioOperation.Complete(NativeMethods.ERROR_OPERATION_ABORTED, 0);
                    return;
                }

                if (_runningOperation != null)
                {
                    if (_nextOperation == null)
                    {
                        _nextOperation = ioOperation;

                        // If there is an active read cancel it
                        if (_runningOperation is AsyncReadOperation)
                        {
                            NativeMethods.HttpTryCancelIO(_handler);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Only one queued operation is allowed");
                    }
                }
                else
                {
                    // we are just starting operation so there would be no
                    // continuation registered
                    var completed = ioOperation.Invoke() != null;

                    // operation went async
                    if (!completed)
                    {
                        _runningOperation = ioOperation;
                    }
                }
            }
        }

        public ValueTask FlushAsync(bool moreData)
        {
            var flush = GetFlushOperation();
            flush.Initialize(_handler, moreData);
            Run(flush);
            return new ValueTask(flush, 0);
        }

        public void NotifyCompletion(int hr, int bytes)
        {
            AsyncIOOperation.AsyncContinuation continuation;
            AsyncIOOperation.AsyncContinuation? nextContinuation = null;

            lock (_context._contextLock)
            {
                Debug.Assert(_runningOperation != null);

                continuation = _runningOperation.Complete(hr, bytes);

                var next = _nextOperation;
                _nextOperation = null;
                _runningOperation = null;

                if (next != null)
                {
                    if (_stopped)
                    {
                        // Abort next operation if IO is stopped
                        nextContinuation = next.Complete(NativeMethods.ERROR_OPERATION_ABORTED, 0);
                    }
                    else
                    {
                        nextContinuation = next.Invoke();

                        // operation went async
                        if (nextContinuation == null)
                        {
                            _runningOperation = next;
                        }
                    }
                }
            }

            continuation.Invoke();
            nextContinuation?.Invoke();
        }

        public void Complete()
        {
            lock (_context._contextLock)
            {
                _stopped = true;

                // Should only call CancelIO if the client hasn't disconnected
                if (!_context.ClientDisconnected)
                {
                    NativeMethods.HttpTryCancelIO(_handler);
                }
            }
        }

        private AsyncReadOperation GetReadOperation() =>
            Interlocked.Exchange(ref _cachedAsyncReadOperation, null) ??
            new AsyncReadOperation(this);

        private AsyncWriteOperation GetWriteOperation() =>
            Interlocked.Exchange(ref _cachedAsyncWriteOperation, null) ??
            new AsyncWriteOperation(this);

        private AsyncFlushOperation GetFlushOperation() =>
            Interlocked.Exchange(ref _cachedAsyncFlushOperation, null) ??
            new AsyncFlushOperation(this);

        private void ReturnOperation(AsyncReadOperation operation)
        {
            Volatile.Write(ref _cachedAsyncReadOperation, operation);
        }

        private void ReturnOperation(AsyncWriteOperation operation)
        {
            Volatile.Write(ref _cachedAsyncWriteOperation, operation);
        }

        private void ReturnOperation(AsyncFlushOperation operation)
        {
            Volatile.Write(ref _cachedAsyncFlushOperation, operation);
        }
    }
}
