// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    internal class NullPipeWriter : PipeWriter
    {
        // Should be large enough for any content attempting to write to the buffer
        private readonly byte[] _buffer = new byte[1024 * 128];

        public override void Advance(int bytes)
        {
        }

        public override void CancelPendingFlush()
        {
        }

        public override void Complete(Exception exception = null)
        {
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<FlushResult>(new FlushResult(false, true));
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            return _buffer;
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            return _buffer;
        }
    }
}
