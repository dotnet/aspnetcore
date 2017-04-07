// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class OutputProducerTests
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

                ((ISocketOutput)socketOutput).Write<object>((buffer, state) =>
                {
                    called = true;
                },
                null);

                Assert.False(called);
            }
        }

        private OutputProducer CreateOutputProducer(PipeOptions pipeOptions)
        {
            var pipe = _pipeFactory.Create(pipeOptions);
            var serviceContext = new TestServiceContext();
            var frame = new Frame<object>(null, new FrameContext { ServiceContext = serviceContext });
            var socketOutput = new OutputProducer(pipe.Writer, frame, "0", serviceContext.Log);

            return socketOutput;
        }
    }
}
