// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class OutputProducerTests : IDisposable
    {
        private readonly PipeFactory _pipeFactory;

        public OutputProducerTests()
        {
            _pipeFactory = new PipeFactory();
        }

        public void Dispose()
        {
            _pipeFactory.Dispose();
        }

        [Fact]
        public void WritesNoopAfterConnectionCloses()
        {
            var pipeOptions = new PipeOptions
            {
                ReaderScheduler = Mock.Of<IScheduler>(),
            };

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

        private OutputProducer CreateOutputProducer(PipeOptions pipeOptions)
        {
            var pipe = _pipeFactory.Create(pipeOptions);
            var serviceContext = new TestServiceContext();
            var socketOutput = new OutputProducer(
                pipe.Reader,
                pipe.Writer,
                "0",
                serviceContext.Log,
                Mock.Of<ITimeoutControl>());

            return socketOutput;
        }
    }
}
