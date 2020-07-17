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
        // This locks access to _completed.
        private readonly object _contextLock = new object();
        private bool _completed = false;

        private readonly Pipe _pipe;

        // https://github.com/dotnet/corefxlab/issues/1334
        // https://github.com/dotnet/aspnetcore/issues/8843
        // Pipelines don't support multiple awaiters on flush. This is temporary until it does.
        // _lastFlushTask field should only be get or set under the _flushLock.
        private readonly object _flushLock = new object();
        private Task _lastFlushTask = Task.CompletedTask;

        public OutputProducer(Pipe pipe)
        {
            _pipe = pipe;
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
            lock (_flushLock)
            {
                _lastFlushTask = _lastFlushTask.IsCompleted ?
                    FlushNowAsync(pipeWriter, cancellationToken) :
                    AwaitLastFlushAndThenFlushAsync(_lastFlushTask, pipeWriter, cancellationToken);

                return _lastFlushTask;
            }
        }

        private Task FlushNowAsync(PipeWriter pipeWriter, CancellationToken cancellationToken)
        {
            var awaitable = pipeWriter.FlushAsync(cancellationToken);
            return awaitable.IsCompleted ? Task.CompletedTask : FlushNowAsyncAwaited(awaitable, cancellationToken);
        }

        private async Task FlushNowAsyncAwaited(ValueTask<FlushResult> awaitable, CancellationToken cancellationToken)
        {
            try
            {
                await awaitable;
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

        private async Task AwaitLastFlushAndThenFlushAsync(Task lastFlushTask, PipeWriter pipeWriter, CancellationToken cancellationToken)
        {
            await lastFlushTask;
            await FlushNowAsync(pipeWriter, cancellationToken);
        }
    }
}
