// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class LibuvConnectionTests
    {
        [Fact]
        public async Task DoesNotEndConnectionOnZeroRead()
        {
            var mockConnectionHandler = new MockConnectionHandler();
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext() { ConnectionHandler = mockConnectionHandler };
            var transport = new LibuvTransport(mockLibuv, transportContext, null);
            var thread = new LibuvThread(transport);

            try
            {
                await thread.StartAsync();
                await thread.PostAsync(_ =>
                {
                    var listenerContext = new ListenerContext(transportContext)
                    {
                        Thread = thread
                    };
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, transportContext.Log);
                    var connection = new LibuvConnection(listenerContext, socket);
                    _ = connection.Start();

                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 0, ref ignored);
                }, (object)null);

                var readAwaitable = mockConnectionHandler.Input.Reader.ReadAsync();
                Assert.False(readAwaitable.IsCompleted);
            }
            finally
            {
                await thread.StopAsync(TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public async Task ConnectionDoesNotResumeAfterSocketCloseIfBackpressureIsApplied()
        {
            var mockConnectionHandler = new MockConnectionHandler();
            mockConnectionHandler.InputOptions.MaximumSizeHigh = 3;
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext() { ConnectionHandler = mockConnectionHandler };
            var transport = new LibuvTransport(mockLibuv, transportContext, null);
            var thread = new LibuvThread(transport);
            // We don't set the output writer scheduler here since we want to run the callback inline
            mockConnectionHandler.OutputOptions.ReaderScheduler = thread;
            Task connectionTask = null;
            try
            {
                await thread.StartAsync();

                // Write enough to make sure back pressure will be applied
                await thread.PostAsync<object>(_ =>
                {
                    var listenerContext = new ListenerContext(transportContext)
                    {
                        Thread = thread
                    };
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, transportContext.Log);
                    var connection = new LibuvConnection(listenerContext, socket);
                    connectionTask = connection.Start();

                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 5, ref ignored);

                }, null);

                // Now assert that we removed the callback from libuv to stop reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now complete the output writer so that the connection closes
                mockConnectionHandler.Output.Writer.Complete();

                await connectionTask.TimeoutAfter(TimeSpan.FromSeconds(10));

                // Assert that we don't try to start reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);
            }
            finally
            {
                await thread.StopAsync(TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public async Task ConnectionDoesNotResumeAfterReadCallbackScheduledAndSocketCloseIfBackpressureIsApplied()
        {
            var mockConnectionHandler = new MockConnectionHandler();
            mockConnectionHandler.InputOptions.MaximumSizeHigh = 3;
            mockConnectionHandler.InputOptions.MaximumSizeLow = 3;
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext() { ConnectionHandler = mockConnectionHandler };
            var transport = new LibuvTransport(mockLibuv, transportContext, null);
            var thread = new LibuvThread(transport);
            var mockScheduler = new Mock<IScheduler>();
            Action backPressure = null;
            mockScheduler.Setup(m => m.Schedule(It.IsAny<Action<object>>(), It.IsAny<object>())).Callback<Action<object>, object>((a, o) =>
            {
                backPressure = () => a(o);
            });
            mockConnectionHandler.InputOptions.WriterScheduler = mockScheduler.Object;
            mockConnectionHandler.OutputOptions.ReaderScheduler = thread;
            Task connectionTask = null;
            try
            {
                await thread.StartAsync();

                // Write enough to make sure back pressure will be applied
                await thread.PostAsync<object>(_ =>
                {
                    var listenerContext = new ListenerContext(transportContext)
                    {
                        Thread = thread
                    };
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, transportContext.Log);
                    var connection = new LibuvConnection(listenerContext, socket);
                    connectionTask = connection.Start();

                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 5, ref ignored);

                }, null);

                // Now assert that we removed the callback from libuv to stop reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now release backpressure by reading the input
                var result = await mockConnectionHandler.Input.Reader.ReadAsync();
                // Calling advance will call into our custom scheduler that captures the back pressure
                // callback
                mockConnectionHandler.Input.Reader.Advance(result.Buffer.End);

                // Cancel the current pending flush
                mockConnectionHandler.Input.Writer.CancelPendingFlush();

                // Now release the back pressure
                await thread.PostAsync(a => a(), backPressure);

                // Assert that we don't try to start reading since the write was cancelled
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now complete the output writer and wait for the connection to close
                mockConnectionHandler.Output.Writer.Complete();

                await connectionTask.TimeoutAfter(TimeSpan.FromSeconds(10));

                // Assert that we don't try to start reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);
            }
            finally
            {
                await thread.StopAsync(TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public async Task DoesNotThrowIfOnReadCallbackCalledWithEOFButAllocCallbackNotCalled()
        {
            var mockConnectionHandler = new MockConnectionHandler();
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext() { ConnectionHandler = mockConnectionHandler };
            var transport = new LibuvTransport(mockLibuv, transportContext, null);
            var thread = new LibuvThread(transport);

            try
            {
                await thread.StartAsync();
                await thread.PostAsync(_ =>
                {
                    var listenerContext = new ListenerContext(transportContext)
                    {
                        Thread = thread
                    };
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, transportContext.Log);
                    var connection = new LibuvConnection(listenerContext, socket);
                    _ = connection.Start();

                    var ignored = new LibuvFunctions.uv_buf_t();
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), TestConstants.EOF, ref ignored);
                }, (object)null);

                var readAwaitable = await mockConnectionHandler.Input.Reader.ReadAsync();
                Assert.True(readAwaitable.IsCompleted);
            }
            finally
            {
                await thread.StopAsync(TimeSpan.FromSeconds(1));
            }
        }
    }
}