// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class OutputProducer
    {
        // This locks access to to all of the below fields
        private readonly object _contextLock = new object();

        private ValueTask<FlushResult> _flushTask;
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

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            _pipe.Reader.CancelPendingRead();
            // Await backpressure
            return FlushAsync(_pipe.Writer, cancellationToken);
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
                _pipe.Writer.Complete();
            }
        }

        public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            lock (_contextLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                _pipe.Writer.Write(buffer.Span);
            }

            return FlushAsync(_pipe.Writer, cancellationToken);
        }

        private Task FlushAsync(PipeWriter pipeWriter, CancellationToken cancellationToken)
        {
            var awaitable = pipeWriter.FlushAsync(cancellationToken);
            if (awaitable.IsCompleted)
            {
                // The flush task can't fail today
                return Task.CompletedTask;
            }
            return FlushAsyncAwaited(awaitable, cancellationToken);
        }

        private async Task FlushAsyncAwaited(ValueTask<FlushResult> awaitable, CancellationToken cancellationToken)
        {
            // https://github.com/dotnet/corefxlab/issues/1334
            // Since the flush awaitable doesn't currently support multiple awaiters
            // we need to use a task to track the callbacks.
            // All awaiters get the same task
            lock (_flushLock)
            {
                _flushTask = awaitable;
                if (_flushTcs == null || _flushTcs.Task.IsCompleted)
                {
                    _flushTcs = new TaskCompletionSource<object>();

                    _flushTask.GetAwaiter().OnCompleted(_flushCompleted);
                }
            }

            try
            {
                await _flushTcs.Task;
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException ex)
            {
                Abort(new ConnectionAbortedException(CoreStrings.ConnectionOrStreamAbortedByCancellationToken, ex));
            }
            catch
            {
                // A canceled token is the only reason flush should ever throw.
                Debug.Assert(false);
            }
        }

        private void OnFlushCompleted()
        {
            try
            {
                _flushTask.GetAwaiter().GetResult();
                _flushTcs.TrySetResult(null);
            }
            catch (Exception exception)
            {
                _flushTcs.TrySetResult(exception);
            }
            finally
            {
                _flushTask = default;
            }
        }
    }
}
