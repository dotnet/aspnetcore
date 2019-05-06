// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class OutputProducerTests : IDisposable
    {
        private readonly MemoryPool<byte> _memoryPool;

        public OutputProducerTests()
        {
            _memoryPool = KestrelMemoryPool.Create();
        }

        public void Dispose()
        {
            _memoryPool.Dispose();
        }

        [Fact]
        public async Task WritesNoopAfterConnectionCloses()
        {
            var pipeOptions = new PipeOptions
            (
                pool: _memoryPool,
                readerScheduler: Mock.Of<PipeScheduler>(),
                writerScheduler: PipeScheduler.Inline,
                useSynchronizationContext: false
            );

            using (var socketOutput = CreateOutputProducer(pipeOptions))
            {
                // Close
                socketOutput.Dispose();

                var called = false;

                await socketOutput.WriteAsync((buffer, state) =>
                {
                    called = true;
                    return 0;
                },
                0,
                default);

                Assert.False(called);
            }
        }

        [Fact]
        public void AbortsTransportEvenAfterDispose()
        {
            var mockConnectionContext = new Mock<ConnectionContext>();

            var outputProducer = CreateOutputProducer(connectionContext: mockConnectionContext.Object);

            outputProducer.Dispose();

            mockConnectionContext.Verify(f => f.Abort(It.IsAny<ConnectionAbortedException>()), Times.Never());

            outputProducer.Abort(null);

            mockConnectionContext.Verify(f => f.Abort(null), Times.Once());

            outputProducer.Abort(null);

            mockConnectionContext.Verify(f => f.Abort(null), Times.Once());
        }

        private Http1OutputProducer CreateOutputProducer(
            PipeOptions pipeOptions = null,
            ConnectionContext connectionContext = null)
        {
            pipeOptions = pipeOptions ?? new PipeOptions();
            connectionContext = connectionContext ?? Mock.Of<ConnectionContext>();

            var pipe = new Pipe(pipeOptions);
            var serviceContext = new TestServiceContext();
            var socketOutput = new Http1OutputProducer(
                pipe.Writer,
                "0",
                connectionContext,
                serviceContext.Log,
                Mock.Of<ITimeoutControl>(),
                Mock.Of<IHttpMinResponseDataRateFeature>());

            return socketOutput;
        }
    }
}
