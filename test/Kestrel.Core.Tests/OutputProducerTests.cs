// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class OutputProducerTests : IDisposable
    {
        private readonly BufferPool _bufferPool;

        public OutputProducerTests()
        {
            _bufferPool = new MemoryPool();
        }

        public void Dispose()
        {
            _bufferPool.Dispose();
        }

        [Fact]
        public void WritesNoopAfterConnectionCloses()
        {
            var pipeOptions = new PipeOptions
            (
                bufferPool:_bufferPool,
                readerScheduler: Mock.Of<IScheduler>()
            );

            using (var socketOutput = CreateOutputProducer(pipeOptions))
            {
                // Close
                socketOutput.Dispose();

                var called = false;

                socketOutput.Write((buffer, state) =>
                {
                    called = true;
                },
                0);

                Assert.False(called);
            }
        }

        private Http1OutputProducer CreateOutputProducer(PipeOptions pipeOptions)
        {
            var pipe = new Pipe(pipeOptions);
            var serviceContext = new TestServiceContext();
            var socketOutput = new Http1OutputProducer(
                pipe.Reader,
                pipe.Writer,
                "0",
                serviceContext.Log,
                Mock.Of<ITimeoutControl>());

            return socketOutput;
        }
    }
}
