// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public class Http1OutputProducer : IHttpOutputProducer
    {
        private static readonly ArraySegment<byte> _continueBytes = new ArraySegment<byte>(Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n"));
        private static readonly byte[] _bytesHttpVersion11 = Encoding.ASCII.GetBytes("HTTP/1.1 ");
        private static readonly byte[] _bytesEndHeaders = Encoding.ASCII.GetBytes("\r\n\r\n");
        private static readonly ArraySegment<byte> _endChunkedResponseBytes = new ArraySegment<byte>(Encoding.ASCII.GetBytes("0\r\n\r\n"));

        private readonly string _connectionId;
        private readonly ITimeoutControl _timeoutControl;
        private readonly IKestrelTrace _log;

        // This locks access to to all of the below fields
        private readonly object _contextLock = new object();

        private bool _completed = false;

        private readonly IPipeWriter _pipeWriter;
        private readonly IPipeReader _outputPipeReader;

        // https://github.com/dotnet/corefxlab/issues/1334
        // Pipelines don't support multiple awaiters on flush
        // this is temporary until it does
        private TaskCompletionSource<object> _flushTcs;
        private readonly object _flushLock = new object();
        private Action _flushCompleted;

        public Http1OutputProducer(
            IPipeReader outputPipeReader,
            IPipeWriter pipeWriter,
            string connectionId,
            IKestrelTrace log,
            ITimeoutControl timeoutControl)
        {
            _outputPipeReader = outputPipeReader;
            _pipeWriter = pipeWriter;
            _connectionId = connectionId;
            _timeoutControl = timeoutControl;
            _log = log;
            _flushCompleted = OnFlushCompleted;
        }

        public Task WriteDataAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return WriteAsync(buffer, cancellationToken);
        }

        public Task WriteStreamSuffixAsync(CancellationToken cancellationToken)
        {
            return WriteAsync(_endChunkedResponseBytes, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return WriteAsync(Constants.EmptyData, cancellationToken);
        }

        public void Write<T>(Action<WritableBuffer, T> callback, T state)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return;
                }

                var buffer = _pipeWriter.Alloc(1);
                callback(buffer, state);
                buffer.Commit();
            }
        }

        public Task WriteAsync<T>(Action<WritableBuffer, T> callback, T state)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                var buffer = _pipeWriter.Alloc(1);
                callback(buffer, state);
                buffer.Commit();
            }

            return FlushAsync();
        }

        public void WriteResponseHeaders(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return;
                }

                var buffer = _pipeWriter.Alloc(1);
                var writer = new WritableBufferWriter(buffer);

                writer.Write(_bytesHttpVersion11);
                var statusBytes = ReasonPhrases.ToStatusBytes(statusCode, reasonPhrase);
                writer.Write(statusBytes);
                responseHeaders.CopyTo(ref writer);
                writer.Write(_bytesEndHeaders);
                buffer.Commit();
            }
        }

        public void Dispose()
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return;
                }

                _log.ConnectionDisconnect(_connectionId);
                _completed = true;
                _pipeWriter.Complete();
            }
        }

        public void Abort(Exception error)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return;
                }

                _log.ConnectionDisconnect(_connectionId);
                _completed = true;

                _outputPipeReader.CancelPendingRead();
                _pipeWriter.Complete(error);
            }
        }

        public Task Write100ContinueAsync(CancellationToken cancellationToken)
        {
            return WriteAsync(_continueBytes, default(CancellationToken));
        }

        private Task WriteAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            var writableBuffer = default(WritableBuffer);
            long bytesWritten = 0;
            lock (_contextLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                writableBuffer = _pipeWriter.Alloc(1);
                var writer = new WritableBufferWriter(writableBuffer);
                if (buffer.Count > 0)
                {
                    writer.Write(buffer.Array, buffer.Offset, buffer.Count);
                    bytesWritten += buffer.Count;
                }

                writableBuffer.Commit();
            }

            return FlushAsync(writableBuffer, bytesWritten, cancellationToken);
        }

        // Single caller, at end of method - so inline
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task FlushAsync(WritableBuffer writableBuffer, long bytesWritten, CancellationToken cancellationToken)
        {
            var awaitable = writableBuffer.FlushAsync(cancellationToken);
            if (awaitable.IsCompleted)
            {
                // The flush task can't fail today
                return Task.CompletedTask;
            }
            return FlushAsyncAwaited(awaitable, bytesWritten, cancellationToken);
        }

        private async Task FlushAsyncAwaited(WritableBufferAwaitable awaitable, long count, CancellationToken cancellationToken)
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

                    awaitable.OnCompleted(_flushCompleted);
                }
            }

            _timeoutControl.StartTimingWrite(count);
            await _flushTcs.Task;
            _timeoutControl.StopTimingWrite();

            cancellationToken.ThrowIfCancellationRequested();
        }

        private void OnFlushCompleted()
        {
            _flushTcs.TrySetResult(null);
        }
    }
}
