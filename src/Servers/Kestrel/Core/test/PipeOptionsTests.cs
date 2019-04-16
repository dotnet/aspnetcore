// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
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
            serviceContext.Scheduler = PipeScheduler.ThreadPool;

            var mockScheduler = Mock.Of<PipeScheduler>();
            var outputPipeOptions = ConnectionDispatcher.GetOutputPipeOptions(serviceContext, KestrelMemoryPool.Create(), readerScheduler: mockScheduler);

            Assert.Equal(expectedMaximumSizeLow, outputPipeOptions.ResumeWriterThreshold);
            Assert.Equal(expectedMaximumSizeHigh, outputPipeOptions.PauseWriterThreshold);
            Assert.Same(mockScheduler, outputPipeOptions.ReaderScheduler);
            Assert.Same(serviceContext.Scheduler, outputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void InputPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxRequestBufferSize = maxRequestBufferSize;
            serviceContext.Scheduler = PipeScheduler.ThreadPool;

            var mockScheduler = Mock.Of<PipeScheduler>();
            var inputPipeOptions = ConnectionDispatcher.GetInputPipeOptions(serviceContext, KestrelMemoryPool.Create(), writerScheduler: mockScheduler);

            Assert.Equal(expectedMaximumSizeLow, inputPipeOptions.ResumeWriterThreshold);
            Assert.Equal(expectedMaximumSizeHigh, inputPipeOptions.PauseWriterThreshold);
            Assert.Same(serviceContext.Scheduler, inputPipeOptions.ReaderScheduler);
            Assert.Same(mockScheduler, inputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void AdaptedInputPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxRequestBufferSize = maxRequestBufferSize;

            var connectionLifetime = new HttpConnection(new HttpConnectionContext
            {
                ServiceContext = serviceContext
            });

            Assert.Equal(expectedMaximumSizeLow, connectionLifetime.AdaptedInputPipeOptions.ResumeWriterThreshold);
            Assert.Equal(expectedMaximumSizeHigh, connectionLifetime.AdaptedInputPipeOptions.PauseWriterThreshold);
            Assert.Same(serviceContext.Scheduler, connectionLifetime.AdaptedInputPipeOptions.ReaderScheduler);
            Assert.Same(PipeScheduler.Inline, connectionLifetime.AdaptedInputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void AdaptedOutputPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxResponseBufferSize = maxRequestBufferSize;

            var connectionLifetime = new HttpConnection(new HttpConnectionContext
            {
                ServiceContext = serviceContext
            });

            Assert.Equal(expectedMaximumSizeLow, connectionLifetime.AdaptedOutputPipeOptions.ResumeWriterThreshold);
            Assert.Equal(expectedMaximumSizeHigh, connectionLifetime.AdaptedOutputPipeOptions.PauseWriterThreshold);
            Assert.Same(PipeScheduler.Inline, connectionLifetime.AdaptedOutputPipeOptions.ReaderScheduler);
            Assert.Same(PipeScheduler.Inline, connectionLifetime.AdaptedOutputPipeOptions.WriterScheduler);
        }
    }
}
