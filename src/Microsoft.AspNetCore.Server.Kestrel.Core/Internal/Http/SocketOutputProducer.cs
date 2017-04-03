// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class SocketOutputProducer : ISocketOutput, IDisposable
    {
        private static readonly ArraySegment<byte> _emptyData = new ArraySegment<byte>(new byte[0]);

        private readonly string _connectionId;
        private readonly IKestrelTrace _log;

        // This locks access to to all of the below fields
        private readonly object _contextLock = new object();

        private bool _cancelled = false;
        private bool _completed = false;

        private readonly IPipeWriter _pipe;
        private readonly Frame _frame;

        // https://github.com/dotnet/corefxlab/issues/1334 
        // Pipelines don't support multiple awaiters on flush
        // this is temporary until it does
        private TaskCompletionSource<object> _flushTcs;
        private readonly object _flushLock = new object();
        private readonly Action _onFlushCallback;

        public SocketOutputProducer(IPipeWriter pipe, Frame frame, string connectionId, IKestrelTrace log)
        {
            _pipe = pipe;
            _frame = frame;
            _connectionId = connectionId;
            _log = log;
            _onFlushCallback = OnFlush;
        }

        public Task WriteAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken,
            bool chunk = false)
        {
            var writableBuffer = default(WritableBuffer);

            lock (_contextLock)
            {
                if (_completed)
                {
                    // TODO: Get actual notification when the consumer stopped from Pipes,
                    // so we know if the socket is fully closed and why (for logging exceptions);
                    _log.ConnectionDisconnectedWrite(_connectionId, buffer.Count, ex: null);
                    return TaskCache.CompletedTask;
                }

                writableBuffer = _pipe.Alloc();

                if (buffer.Count > 0)
                {
                    if (chunk)
                    {
                        ChunkWriter.WriteBeginChunkBytes(ref writableBuffer, buffer.Count);
                    }

                    writableBuffer.WriteFast(buffer);

                    if (chunk)
                    {
                        ChunkWriter.WriteEndChunkBytes(ref writableBuffer);
                    }
                }

                writableBuffer.Commit();
            }

            return FlushAsync(writableBuffer);
        }

        private Task FlushAsync(WritableBuffer writableBuffer)
        {
            var awaitable = writableBuffer.FlushAsync();
            if (awaitable.IsCompleted)
            {
                // The flush task can't fail today
                return TaskCache.CompletedTask;
            }
            return FlushAsyncAwaited(awaitable);
        }

        private Task FlushAsyncAwaited(WritableBufferAwaitable awaitable)
        {
            // https://github.com/dotnet/corefxlab/issues/1334 
            // Since the flush awaitable doesn't currently support multiple awaiters
            // we need to use a task to track the callbacks.
            // All awaiters get the same task
            lock (_flushLock)
            {
                if (_flushTcs == null || _flushTcs.Task.IsCompleted)
                {
                    _flushTcs = new TaskCompletionSource<object>();

                    awaitable.OnCompleted(_onFlushCallback);
                }
            }

            return _flushTcs.Task;
        }

        private void OnFlush()
        {
            _flushTcs.TrySetResult(null);
        }

        void ISocketOutput.Write(ArraySegment<byte> buffer, bool chunk)
        {
            WriteAsync(buffer, default(CancellationToken), chunk).GetAwaiter().GetResult();
        }

        Task ISocketOutput.WriteAsync(ArraySegment<byte> buffer, bool chunk, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _frame.Abort();
                _cancelled = true;
                return Task.FromCanceled(cancellationToken);
            }
            else if (_cancelled)
            {
                return TaskCache.CompletedTask;
            }

            return WriteAsync(buffer, cancellationToken, chunk);
        }

        void ISocketOutput.Flush()
        {
            WriteAsync(_emptyData, default(CancellationToken)).GetAwaiter().GetResult();
        }

        Task ISocketOutput.FlushAsync(CancellationToken cancellationToken)
        {
            return WriteAsync(_emptyData, cancellationToken);
        }

        void ISocketOutput.Write<T>(Action<WritableBuffer, T> callback, T state)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return;
                }

                var buffer = _pipe.Alloc();
                callback(buffer, state);
                buffer.Commit();
            }
        }

        public void Dispose()
        {
            lock (_contextLock)
            {
                _completed = true;
                _pipe.Complete();
            }
        }
    }
}
