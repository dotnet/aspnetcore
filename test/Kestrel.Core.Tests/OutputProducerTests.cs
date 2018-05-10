// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Connections.Features;
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
        public void WritesNoopAfterConnectionCloses()
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

                socketOutput.Write((buffer, state) =>
                {
                    called = true;
                    return 0;
                },
                0);

                Assert.False(called);
            }
        }

        [Fact]
        public void AbortsTransportEvenAfterDispose()
        {
            var mockLifetimeFeature = new Mock<IConnectionLifetimeFeature>();

            var outputProducer = CreateOutputProducer(lifetimeFeature: mockLifetimeFeature.Object);

            outputProducer.Dispose();

            mockLifetimeFeature.Verify(f => f.Abort(), Times.Never());

            outputProducer.Abort(null);

            mockLifetimeFeature.Verify(f => f.Abort(), Times.Once());

            outputProducer.Abort(null);

            mockLifetimeFeature.Verify(f => f.Abort(), Times.Once());
        }

        private Http1OutputProducer CreateOutputProducer(
            PipeOptions pipeOptions = null,
            IConnectionLifetimeFeature lifetimeFeature = null)
        {
            pipeOptions = pipeOptions ?? new PipeOptions();
            lifetimeFeature = lifetimeFeature ?? Mock.Of<IConnectionLifetimeFeature>();

            var pipe = new Pipe(pipeOptions);
            var serviceContext = new TestServiceContext();
            var socketOutput = new Http1OutputProducer(
                pipe.Reader,
                pipe.Writer,
                "0",
                serviceContext.Log,
                Mock.Of<ITimeoutControl>(),
                lifetimeFeature,
                Mock.Of<IBytesWrittenFeature>());

            return socketOutput;
        }
    }
}
