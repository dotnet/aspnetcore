// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
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

            var connectionHandler = new ConnectionHandler<object>(listenOptions: null, serviceContext: serviceContext, application: null);
            var mockScheduler = Mock.Of<IScheduler>();
            var outputPipeOptions = connectionHandler.GetOutputPipeOptions(readerScheduler: mockScheduler);

            Assert.Equal(expectedMaximumSizeLow, outputPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, outputPipeOptions.MaximumSizeHigh);
            Assert.Same(mockScheduler, outputPipeOptions.ReaderScheduler);
            Assert.Same(serviceContext.ThreadPool, outputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void InputPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxRequestBufferSize = maxRequestBufferSize;
            serviceContext.ThreadPool = new LoggingThreadPool(null);

            var connectionHandler = new ConnectionHandler<object>(listenOptions: null, serviceContext: serviceContext, application: null);
            var mockScheduler = Mock.Of<IScheduler>();
            var inputPipeOptions = connectionHandler.GetInputPipeOptions(writerScheduler: mockScheduler);

            Assert.Equal(expectedMaximumSizeLow, inputPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, inputPipeOptions.MaximumSizeHigh);
            Assert.Same(serviceContext.ThreadPool, inputPipeOptions.ReaderScheduler);
            Assert.Same(mockScheduler, inputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void AdaptedInputPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxRequestBufferSize = maxRequestBufferSize;

            var connectionLifetime = new FrameConnection(new FrameConnectionContext
            {
                ServiceContext = serviceContext
            });

            Assert.Equal(expectedMaximumSizeLow, connectionLifetime.AdaptedInputPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, connectionLifetime.AdaptedInputPipeOptions.MaximumSizeHigh);
            Assert.Same(serviceContext.ThreadPool, connectionLifetime.AdaptedInputPipeOptions.ReaderScheduler);
            Assert.Same(InlineScheduler.Default, connectionLifetime.AdaptedInputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void AdaptedOutputPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxResponseBufferSize = maxRequestBufferSize;

            var connectionLifetime = new FrameConnection(new FrameConnectionContext
            {
                ServiceContext = serviceContext
            });

            Assert.Equal(expectedMaximumSizeLow, connectionLifetime.AdaptedOutputPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, connectionLifetime.AdaptedOutputPipeOptions.MaximumSizeHigh);
            Assert.Same(InlineScheduler.Default, connectionLifetime.AdaptedOutputPipeOptions.ReaderScheduler);
            Assert.Same(InlineScheduler.Default, connectionLifetime.AdaptedOutputPipeOptions.WriterScheduler);
        }
    }
}
