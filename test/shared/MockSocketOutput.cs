// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Testing
{
    public class MockSocketOutput : ISocketOutput
    {
        public MockSocketOutput()
        {
        }

        public void Write(ArraySegment<byte> buffer, bool chunk = false)
        {
        }

        public Task WriteAsync(ArraySegment<byte> buffer, bool chunk = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return TaskCache.CompletedTask;
        }

        public void Flush()
        {
        }

        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return TaskCache.CompletedTask;
        }

        public void Write<T>(Action<WritableBuffer, T> write, T state)
        {

        }
    }
}
