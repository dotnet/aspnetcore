// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class OutputProducer
    {
        private static readonly ArraySegment<byte> _emptyData = new ArraySegment<byte>(new byte[0]);

        // This locks access to to all of the below fields
        private readonly object _contextLock = new object();

        private bool _completed = false;

        private readonly IPipe _pipe;

        // https://github.com/dotnet/corefxlab/issues/1334
        // Pipelines don't support multiple awaiters on flush
        // this is temporary until it does
        private TaskCompletionSource<object> _flushTcs;
        private readonly object _flushLock = new object();
        private Action _flushCompleted;

        public OutputProducer(IPipe pipe)
        {
            _pipe = pipe;
            _flushCompleted = OnFlushCompleted;
        }

        public IPipeReader Reader => _pipe.Reader;

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return WriteAsync(_emptyData, cancellationToken);
        }

        public void Dispose()
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
                _pipe.Writer.Complete();
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

                _completed = true;

                _pipe.Reader.CancelPendingRead();
                _pipe.Writer.Complete(error);
            }
        }

        public Task WriteAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            var writableBuffer = default(WritableBuffer);

            lock (_contextLock)
            {
                if (_completed)
                {
                    throw new ObjectDisposedException("Response is already completed");
                }

                writableBuffer = _pipe.Writer.Alloc(1);
                // TODO obsolete
#pragma warning disable CS0618 // Type or member is obsolete
                var writer = new WritableBufferWriter(writableBuffer);
#pragma warning restore CS0618 // Type or member is obsolete
                if (buffer.Count > 0)
                {
                    writer.Write(buffer.Array, buffer.Offset, buffer.Count);
                }

                writableBuffer.Commit();
            }

            return FlushAsync(writableBuffer, cancellationToken);
        }

        private Task FlushAsync(WritableBuffer writableBuffer,
            CancellationToken cancellationToken)
        {
            var awaitable = writableBuffer.FlushAsync(cancellationToken);
            if (awaitable.IsCompleted)
            {
                // The flush task can't fail today
                return Task.CompletedTask;
            }
            return FlushAsyncAwaited(awaitable, cancellationToken);
        }

        private async Task FlushAsyncAwaited(WritableBufferAwaitable awaitable, CancellationToken cancellationToken)
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

            await _flushTcs.Task;

            cancellationToken.ThrowIfCancellationRequested();
        }

        private void OnFlushCompleted()
        {
            _flushTcs.TrySetResult(null);
        }
    }
}
