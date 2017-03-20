// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ListenerContextTests
    {
        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(0, 1, 1)]
        [InlineData(null, 0, 0)]
        public void LibuvOutputPipeOptionsConfiguredCorrectly(long? maxResponseBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxResponseBufferSize = maxResponseBufferSize;
            serviceContext.ThreadPool = new LoggingThreadPool(null);

            var listenerContext = new ListenerContext(serviceContext)
            {
                Thread = new KestrelThread(new KestrelEngine(null, serviceContext))
            };

            Assert.Equal(expectedMaximumSizeLow, listenerContext.LibuvOutputPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, listenerContext.LibuvOutputPipeOptions.MaximumSizeHigh);
            Assert.Same(listenerContext.Thread, listenerContext.LibuvOutputPipeOptions.ReaderScheduler);
            Assert.Same(serviceContext.ThreadPool, listenerContext.LibuvOutputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void LibuvInputPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxRequestBufferSize = maxRequestBufferSize;
            serviceContext.ThreadPool = new LoggingThreadPool(null);

            var listenerContext = new ListenerContext(serviceContext)
            {
                Thread = new KestrelThread(new KestrelEngine(null, serviceContext))
            };

            Assert.Equal(expectedMaximumSizeLow, listenerContext.LibuvInputPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, listenerContext.LibuvInputPipeOptions.MaximumSizeHigh);
            Assert.Same(serviceContext.ThreadPool, listenerContext.LibuvInputPipeOptions.ReaderScheduler);
            Assert.Same(listenerContext.Thread, listenerContext.LibuvInputPipeOptions.WriterScheduler);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(null, 0, 0)]
        public void AdaptedPipeOptionsConfiguredCorrectly(long? maxRequestBufferSize, long expectedMaximumSizeLow, long expectedMaximumSizeHigh)
        {
            var serviceContext = new TestServiceContext();
            serviceContext.ServerOptions.Limits.MaxRequestBufferSize = maxRequestBufferSize;

            var listenerContext = new ListenerContext(serviceContext);

            Assert.Equal(expectedMaximumSizeLow, listenerContext.AdaptedPipeOptions.MaximumSizeLow);
            Assert.Equal(expectedMaximumSizeHigh, listenerContext.AdaptedPipeOptions.MaximumSizeHigh);
            Assert.Same(InlineScheduler.Default, listenerContext.AdaptedPipeOptions.ReaderScheduler);
            Assert.Same(InlineScheduler.Default, listenerContext.AdaptedPipeOptions.WriterScheduler);
        }
    }
}
