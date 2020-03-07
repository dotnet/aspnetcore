// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared
{
    public class TestPipeWriter : PipeWriter
    {
        // huge buffer that should be large enough for writing any content
        private readonly byte[] _buffer = new byte[10000];

        public bool ForceAsync { get; set; }

        public override void Advance(int bytes)
        {
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            return _buffer;
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            return _buffer;
        }

        public override void CancelPendingFlush()
        {
            throw new NotImplementedException();
        }

        public override void Complete(Exception exception = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (!ForceAsync)
            {
                return default;
            }

            return new ValueTask<FlushResult>(ForceAsyncResult());
        }

        public async Task<FlushResult> ForceAsyncResult()
        {
            return await Task.FromResult<FlushResult>(default).ForceAsync();
        }
    }
}
