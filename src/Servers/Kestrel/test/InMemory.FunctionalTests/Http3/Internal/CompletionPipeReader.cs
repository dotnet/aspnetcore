// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    internal sealed class CompletionPipeReader : PipeReader
    {
        private readonly PipeReader _inner;

        public bool IsComplete { get; private set; }
        public Exception CompleteException { get; private set; }

        public CompletionPipeReader(PipeReader inner)
        {
            _inner = inner;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            _inner.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            _inner.AdvanceTo(consumed, examined);
        }

        public override ValueTask CompleteAsync(Exception exception = null)
        {
            IsComplete = true;
            CompleteException = exception;
            return _inner.CompleteAsync(exception);
        }

        public override void Complete(Exception exception = null)
        {
            IsComplete = true;
            CompleteException = exception;
            _inner.Complete(exception);
        }

        public override void CancelPendingRead()
        {
            _inner.CancelPendingRead();
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            return _inner.ReadAsync(cancellationToken);
        }

        public override bool TryRead(out ReadResult result)
        {
            return _inner.TryRead(out result);
        }
    }
}
