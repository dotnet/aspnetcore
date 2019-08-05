// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.TestHost
{
    internal class ResponseBodyPipeWriter : PipeWriter
    {
        private readonly Func<Task> _onFirstWriteAsync;
        private readonly Pipe _pipe;

        private bool _firstWrite;
        private bool _complete;

        internal ResponseBodyPipeWriter(Pipe pipe, Func<Task> onFirstWriteAsync)
        {
            _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
            _onFirstWriteAsync = onFirstWriteAsync ?? throw new ArgumentNullException(nameof(onFirstWriteAsync));
            _firstWrite = true;
        }

        public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckNotComplete();

            await FirstWriteAsync();
            return await _pipe.Writer.FlushAsync(cancellationToken);
        }

        private Task FirstWriteAsync()
        {
            if (_firstWrite)
            {
                _firstWrite = false;
                return _onFirstWriteAsync();
            }
            return Task.CompletedTask;
        }

        internal void Abort(Exception innerException)
        {
            Contract.Requires(innerException != null);
            _complete = true;
            _pipe.Writer.Complete(new IOException(string.Empty, innerException));
        }

        internal void Complete()
        {
            if (_complete)
            {
                return;
            }

            // Throw for further writes, but not reads. Allow reads to drain the buffered data and then return 0 for further reads.
            _complete = true;
            _pipe.Writer.Complete();
        }

        private void CheckNotComplete()
        {
            if (_complete)
            {
                throw new IOException("The request was aborted or the pipeline has finished.");
            }
        }

        public override void Complete(Exception exception = null)
        {
            // No-op in the non-error case
            if (exception != null)
            {
                Abort(exception);
            }
        }

        public override void CancelPendingFlush() => _pipe.Writer.CancelPendingFlush();

        public override void Advance(int bytes)
        {
            CheckNotComplete();
            _pipe.Writer.Advance(bytes);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckNotComplete();
            return _pipe.Writer.GetMemory(sizeHint);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckNotComplete();
            return _pipe.Writer.GetSpan(sizeHint);
        }
    }
}
