// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class SocketOutput : ISocketOutput
    {
        private static readonly ArraySegment<byte> _emptyData = new ArraySegment<byte>(new byte[0]);

        private readonly KestrelThread _thread;
        private readonly UvStreamHandle _socket;
        private readonly Connection _connection;
        private readonly string _connectionId;
        private readonly IKestrelTrace _log;

        // This locks access to to all of the below fields
        private readonly object _contextLock = new object();

        private bool _cancelled = false;
        private bool _completed = false;
        private Exception _lastWriteError;
        private readonly WriteReqPool _writeReqPool;
        private readonly IPipe _pipe;
        private Task _writingTask;

        // https://github.com/dotnet/corefxlab/issues/1334 
        // Pipelines don't support multiple awaiters on flush
        // this is temporary until it does
        private TaskCompletionSource<object> _flushTcs;
        private readonly object _flushLock = new object();
        private readonly Action _onFlushCallback;

        public SocketOutput(
            IPipe pipe,
            KestrelThread thread,
            UvStreamHandle socket,
            Connection connection,
            string connectionId,
            IKestrelTrace log)
        {
            _pipe = pipe;
            // We need to have empty pipe at this moment so callback
            // get's scheduled
            _writingTask = StartWrites();
            _thread = thread;
            _socket = socket;
            _connection = connection;
            _connectionId = connectionId;
            _log = log;
            _writeReqPool = thread.WriteReqPool;
            _onFlushCallback = OnFlush;
        }

        public async Task WriteAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken,
            bool chunk = false)
        {
            var writableBuffer = default(WritableBuffer);

            lock (_contextLock)
            {
                if (_socket.IsClosed)
                {
                    _log.ConnectionDisconnectedWrite(_connectionId, buffer.Count, _lastWriteError);

                    return;
                }

                if (_completed)
                {
                    return;
                }

                writableBuffer = _pipe.Writer.Alloc();

                if (buffer.Count > 0)
                {
                    if (chunk)
                    {
                        ChunkWriter.WriteBeginChunkBytes(ref writableBuffer, buffer.Count);
                    }

                    writableBuffer.Write(buffer);

                    if (chunk)
                    {
                        ChunkWriter.WriteEndChunkBytes(ref writableBuffer);
                    }
                }

                writableBuffer.Commit();
            }

            await FlushAsync(writableBuffer);
        }

        public void End(ProduceEndType endType)
        {
            if (endType == ProduceEndType.SocketShutdown)
            {
                // Graceful shutdown
                _pipe.Reader.CancelPendingRead();
            }

            lock (_contextLock)
            {
                _completed = true;
            }

            // We're done writing
            _pipe.Writer.Complete();
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
                _connection.AbortAsync();
                _cancelled = true;
                return TaskUtilities.GetCancelledTask(cancellationToken);
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

        public WritableBuffer Alloc()
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    // This is broken
                    return default(WritableBuffer);
                }

                return _pipe.Writer.Alloc();
            }
        }

        public async Task StartWrites()
        {
            while (true)
            {
                var result = await _pipe.Reader.ReadAsync();
                var buffer = result.Buffer;

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        var writeReq = _writeReqPool.Allocate();
                        var writeResult = await writeReq.WriteAsync(_socket, buffer);
                        _writeReqPool.Return(writeReq);

                        // REVIEW: Locking here, do we need to take the context lock?
                        OnWriteCompleted(writeResult.Status, writeResult.Error);
                    }

                    if (result.IsCancelled)
                    {
                        // Send a FIN
                        await ShutdownAsync();
                    }

                    if (buffer.IsEmpty && result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    _pipe.Reader.Advance(result.Buffer.End);
                }
            }

            // We're done reading
            _pipe.Reader.Complete();

            _socket.Dispose();
            _connection.OnSocketClosed();
            _log.ConnectionStop(_connectionId);
        }

        private void OnWriteCompleted(int writeStatus, Exception writeError)
        {
            // Called inside _contextLock
            var status = writeStatus;
            var error = writeError;

            if (error != null)
            {
                // Abort the connection for any failed write
                // Queued on threadpool so get it in as first op.
                _connection.AbortAsync();
                _cancelled = true;
                _lastWriteError = error;
            }

            if (error == null)
            {
                _log.ConnectionWriteCallback(_connectionId, status);
            }
            else
            {
                // Log connection resets at a lower (Debug) level.
                if (status == Constants.ECONNRESET)
                {
                    _log.ConnectionReset(_connectionId);
                }
                else
                {
                    _log.ConnectionError(_connectionId, error);
                }
            }
        }

        private Task ShutdownAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            _log.ConnectionWriteFin(_connectionId);

            var shutdownReq = new UvShutdownReq(_log);
            shutdownReq.Init(_thread.Loop);
            shutdownReq.Shutdown(_socket, (req, status, state) =>
            {
                req.Dispose();
                _log.ConnectionWroteFin(_connectionId, status);

                tcs.TrySetResult(null);
            },
            this);

            return tcs.Task;
        }
    }
}
