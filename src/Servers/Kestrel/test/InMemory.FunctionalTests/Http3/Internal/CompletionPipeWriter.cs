// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    internal sealed class CompletionPipeWriter : PipeWriter
    {
        private readonly PipeWriter _inner;

        public bool IsComplete { get; private set; }
        public Exception CompleteException { get; private set; }

        public CompletionPipeWriter(PipeWriter inner)
        {
            _inner = inner;
        }

        public override void Advance(int bytes)
        {
            _inner.Advance(bytes);
        }

        public override void CancelPendingFlush()
        {
            _inner.CancelPendingFlush();
        }

        public override void Complete(Exception exception = null)
        {
            IsComplete = true;
            CompleteException = exception;
            _inner.Complete(exception);
        }

        public override ValueTask CompleteAsync(Exception exception = null)
        {
            IsComplete = true;
            CompleteException = exception;
            return _inner.CompleteAsync(exception);
        }

        public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            return _inner.WriteAsync(source, cancellationToken);
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            return _inner.GetMemory(sizeHint);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            return _inner.GetSpan(sizeHint);
        }
    }
}
