// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{
    public class MockSocketOuptut : ISocketOutput
    {
        public void ProducingComplete(MemoryPoolIterator end)
        {
        }

        public MemoryPoolIterator ProducingStart()
        {
            return new MemoryPoolIterator();
        }

        public void Write(ArraySegment<byte> buffer, bool chunk = false)
        {
        }

        public Task WriteAsync(ArraySegment<byte> buffer, bool chunk = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return TaskCache.CompletedTask;
        }
    }
}
