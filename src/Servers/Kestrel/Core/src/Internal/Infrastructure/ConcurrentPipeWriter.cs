// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
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



        private TaskCompletionSource<FlushResult> _currentFlushTcs;

        public ConcurrentPipeWriter(PipeWriter innerPipeWriter)
        {
            _innerPipeWriter = innerPipeWriter;
        }

        public override void Advance(int bytes)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null)
                {
                    _innerPipeWriter.Advance(bytes);
                    return;
                }

                throw new NotImplementedException();
            }
        }

        // This is not exposed to end users. Throw so we find out if we ever start calling this.
        public override void CancelPendingFlush()
        {
            throw new NotImplementedException();
        }

        public override void Complete(Exception exception = null)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null)
                {
                    _innerPipeWriter.Complete(exception);
                    return;
                }

                throw new NotImplementedException();
            }
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_currentFlushTcs != null)
                {
                    return new ValueTask<FlushResult>(_currentFlushTcs.Task);
                }

                var flushTask = _innerPipeWriter.FlushAsync(cancellationToken);

                if (flushTask.IsCompletedSuccessfully)
                {
                    return flushTask;
                }

                return FlushAsyncAwaitedUnsynchronized(flushTask);
            }
        }

        private async ValueTask<FlushResult> FlushAsyncAwaitedUnsynchronized(ValueTask<FlushResult> flushTask)
        {
            // TODO: Propogate IsCanceled when we do multiple flushes in a loop.
            // If IsCanceled and more data pending to flush, complete currentTcs with canceled flush task,
            // But rekick the FlushAsync loop.
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null)
                {
                    return _innerPipeWriter.GetMemory(sizeHint);
                }

                throw new NotImplementedException();
            }
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            lock (_sync)
            {
                if (_currentFlushTcs == null)
                {
                    return _innerPipeWriter.GetSpan(sizeHint);
                }

                throw new NotImplementedException();
            }
        }

        // This is not exposed to end users. Throw so we find out if we ever start calling this.
        public override void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}
