// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    /// <summary>
    /// This wraps PipeWriter.FlushAsync() in a way that allows multiple awaiters making it safe to call from publicly
    /// exposed Stream implementations while also tracking response data rate.
    /// </summary>
    public class TimingPipeFlusher
    {
        private readonly PipeWriter _writer;
        private readonly ITimeoutControl _timeoutControl;
        private readonly IKestrelTrace _log;

        private readonly object _flushLock = new object();
        private Task _lastFlushTask = Task.CompletedTask;

        public TimingPipeFlusher(
            PipeWriter writer,
            ITimeoutControl timeoutControl,
            IKestrelTrace log)
        {
            _writer = writer;
            _timeoutControl = timeoutControl;
            _log = log;
        }

        public Task FlushAsync()
        {
            return FlushAsync(outputAborter: null, cancellationToken: default);
        }

        public Task FlushAsync(IHttpOutputAborter outputAborter, CancellationToken cancellationToken)
        {
            return FlushAsync(minRate: null, count: 0, outputAborter: outputAborter, cancellationToken: cancellationToken);
        }

        public Task FlushAsync(MinDataRate minRate, long count)
        {
            return FlushAsync(minRate, count, outputAborter: null, cancellationToken: default);
        }

        public Task FlushAsync(MinDataRate minRate, long count, IHttpOutputAborter outputAborter, CancellationToken cancellationToken)
        {
            var flushValueTask = _writer.FlushAsync(cancellationToken);

            if (minRate != null)
            {
                _timeoutControl.BytesWrittenToBuffer(minRate, count);
            }

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

                return TimeFlushAsync(minRate, count, outputAborter, cancellationToken);
            }
        }

        private async Task TimeFlushAsync(MinDataRate minRate, long count, IHttpOutputAborter outputAborter, CancellationToken cancellationToken)
        {
            if (minRate != null)
            {
                _timeoutControl.StartTimingWrite();
            }

            try
            {
                await _lastFlushTask;
            }
            catch (OperationCanceledException ex) when (outputAborter != null)
            {
                outputAborter.Abort(new ConnectionAbortedException(CoreStrings.ConnectionOrStreamAbortedByCancellationToken, ex));
            }
            catch (Exception ex)
            {
                // A canceled token is the only reason flush should ever throw.
                _log.LogError(0, ex, $"Unexpected exception in {nameof(TimingPipeFlusher)}.{nameof(TimeFlushAsync)}.");
            }

            if (minRate != null)
            {
                _timeoutControl.StopTimingWrite();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
