// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    /// <summary>
    /// This wraps PipeWriter.FlushAsync() in a way that allows multiple awaiters making it safe to call from publicly
    /// exposed Stream implementations.
    /// </summary>
    public class StreamSafePipeFlusher
    {
        private readonly PipeWriter _writer;
        private readonly ITimeoutControl _timeoutControl;
        private readonly object _flushLock = new object();

        private Task _lastFlushTask = Task.CompletedTask;

        public StreamSafePipeFlusher(
            PipeWriter writer,
            ITimeoutControl timeoutControl)
        {
            _writer = writer;
            _timeoutControl = timeoutControl;
        }

        public Task FlushAsync(long count = 0, IHttpOutputProducer outputProducer = null, CancellationToken cancellationToken = default)
        {
            var flushValueTask = _writer.FlushAsync(cancellationToken);

            if (flushValueTask.IsCompletedSuccessfully)
            {
                return Task.CompletedTask;
            }

            // https://github.com/dotnet/corefxlab/issues/1334
            // Pipelines don't support multiple awaiters on flush.
            // While it's acceptable to call PipeWriter.FlushAsync again before the last FlushAsync completes,
            // it is not acceptable to attach a new continuation (via await, AsTask(), etc..). In this case,
            // we find previous flush Task which still accounts for any newly committed bytes and await that.
            lock (_flushLock)
            {
                if (_lastFlushTask.IsCompleted)
                {
                    _lastFlushTask = flushValueTask.AsTask();
                }

                return TimeFlushAsync(count, outputProducer, cancellationToken);
            }
        }

        private async Task TimeFlushAsync(long count, IHttpOutputProducer outputProducer, CancellationToken cancellationToken)
        {
            _timeoutControl.StartTimingWrite(count);

            try
            {
                await _lastFlushTask;
            }
            catch (OperationCanceledException ex)
            {
                outputProducer.Abort(new ConnectionAbortedException(CoreStrings.ConnectionOrStreamAbortedByCancellationToken, ex));
            }
            catch
            {
                // A canceled token is the only reason flush should ever throw.
            }

            _timeoutControl.StopTimingWrite();

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
