// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    /// <summary>
    /// Wraps a PipeWriter so you can start appending more data to the pipe prior to the previous flush completing.
    /// </summary>
    internal class ConcurrentPipeWriter : PipeWriter
    {
        private readonly object _sync = new object();
        private readonly PipeWriter _innerPipeWriter;
        private Task _lastFlushTask = Task.CompletedTask;

        public ConcurrentPipeWriter(PipeWriter innerPipeWriter)
        {
            _innerPipeWriter = innerPipeWriter;
        }

        public override void Advance(int bytes)
        {
            lock (_sync)
            {
                if (_lastFlushTask.IsCompletedSuccessfully)
                {
                    _innerPipeWriter.Advance(bytes);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public override void CancelPendingFlush()
        {
            _innerPipeWriter.CancelPendingFlush();
        }

        public override void Complete(Exception exception = null)
        {
            lock (_sync)
            {
                if (_lastFlushTask.IsCompletedSuccessfully)
                {
                    _innerPipeWriter.Complete(exception);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_lastFlushTask.IsCompletedSuccessfully)
                {
                    var flushValueTask = _innerPipeWriter.FlushAsync(cancellationToken);

                    if (flushValueTask.IsCompletedSuccessfully)
                    {
                        _lastFlushTask = Task.CompletedTask;
                        return flushValueTask;
                    }

                    // Use a local to avoid an explicit cast from Task to Task<FlushResult> when calling the ValueTask ctor.
                    var localFlushTask = flushValueTask.AsTask();
                    _lastFlushTask = flushValueTask.AsTask();

                    // The PipeWriter's ValueTask<FlushResult> cannot be awaited twice, so use the created Task.
                    return new ValueTask<FlushResult>(localFlushTask);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            lock (_sync)
            {
                if (_lastFlushTask.IsCompletedSuccessfully)
                {
                    return _innerPipeWriter.GetMemory(sizeHint);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            lock (_sync)
            {
                if (_lastFlushTask.IsCompletedSuccessfully)
                {
                    return _innerPipeWriter.GetSpan(sizeHint);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        // This is not exposed to end users. Throw so we find out if we ever start calling this.
        public override void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}
