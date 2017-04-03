// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class PipeOptionsTests
    {
        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(0, 1, 1)]
        [InlineData(null, 0, 0)]
        public void OutputPipeOptionsConfiguredCorrectly(long? maxResponseBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxResponseBufferSize = maxResponseBufferSize;
            serviceContext.ThreadPool = new LoggingThreadPool(null);

            var connectionHandler = new ConnectionHandler<object>(serviceContext, application: null);
            var mockScheduler = Mock.Of<IScheduler>();
            var outputPipeOptions = connectionHandler.GetOutputPipeOptions(mockScheduler);

            Assert.Equal(expectedMaximumSizeLow, outputPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, outputPipeOptions.MaximumSizeHigh);
            Assert.Same(mockScheduler, outputPipeOptions.ReaderScheduler);
            Assert.Same(serviceContext.ThreadPool, outputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void LibuvInputPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxRequestBufferSize = maxRequestBufferSize;
            serviceContext.ThreadPool = new LoggingThreadPool(null);

            var connectionHandler = new ConnectionHandler<object>(serviceContext, application: null);
            var mockScheduler = Mock.Of<IScheduler>();
            var inputPipeOptions = connectionHandler.GetInputPipeOptions(mockScheduler);

            Assert.Equal(expectedMaximumSizeLow, inputPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, inputPipeOptions.MaximumSizeHigh);
            Assert.Same(serviceContext.ThreadPool, inputPipeOptions.ReaderScheduler);
            Assert.Same(mockScheduler, inputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void AdaptedPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxRequestBufferSize = maxRequestBufferSize;

            var connectionLifetime = new FrameConnection(new FrameConnectionContext
            {
                ServiceContext = serviceContext
            });

            Assert.Equal(expectedMaximumSizeLow, connectionLifetime.AdaptedPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, connectionLifetime.AdaptedPipeOptions.MaximumSizeHigh);
            Assert.Same(InlineScheduler.Default, connectionLifetime.AdaptedPipeOptions.ReaderScheduler);
            Assert.Same(InlineScheduler.Default, connectionLifetime.AdaptedPipeOptions.WriterScheduler);
        }
    }
}
