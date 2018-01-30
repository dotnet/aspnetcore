// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
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

        private readonly Pipe _pipe;

        // https://github.com/dotnet/corefxlab/issues/1334
        // Pipelines don't support multiple awaiters on flush
        // this is temporary until it does
        private TaskCompletionSource<object> _flushTcs;
        private readonly object _flushLock = new object();
        private Action _flushCompleted;

        public OutputProducer(Pipe pipe)
        {
            _pipe = pipe;
            _flushCompleted = OnFlushCompleted;
        }

        public PipeReader Reader => _pipe.Reader;

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
            lock (_contextLock)
            {
                if (_completed)
                {
                    throw new ObjectDisposedException("Response is already completed");
                }

                _pipe.Writer.Write(new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count));
            }

            return FlushAsync(_pipe.Writer, cancellationToken);
        }

        private Task FlushAsync(PipeWriter pipeWriter,
            CancellationToken cancellationToken)
        {
            var awaitable = pipeWriter.FlushAsync(cancellationToken);
            if (awaitable.IsCompleted)
            {
                // The flush task can't fail today
                return Task.CompletedTask;
            }
            return FlushAsyncAwaited(awaitable, cancellationToken);
        }

        private async Task FlushAsyncAwaited(ValueAwaiter<FlushResult> awaitable, CancellationToken cancellationToken)
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
