// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
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
            var mockConnectionDispatcher = new MockConnectionDispatcher();
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext { ConnectionDispatcher = mockConnectionDispatcher };
            var transport = new LibuvTransport(mockLibuv, transportContext, null);
            var thread = new LibuvThread(transport);
            Task connectionTask = null;
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
                    var connection = new LibuvConnection(socket, listenerContext.TransportContext.Log, thread, null, null);
                    listenerContext.TransportContext.ConnectionDispatcher.OnConnection(connection);
                    connectionTask = connection.Start();

                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 0, ref ignored);
                }, (object)null);

                var readAwaitable = mockConnectionDispatcher.Input.Reader.ReadAsync();
                Assert.False(readAwaitable.IsCompleted);
            }
            finally
            {
                mockConnectionDispatcher.Input.Reader.Complete();
                mockConnectionDispatcher.Output.Writer.Complete();
                await connectionTask;

                await thread.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public async Task ConnectionDoesNotResumeAfterSocketCloseIfBackpressureIsApplied()
        {
            var mockConnectionDispatcher = new MockConnectionDispatcher();
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext { ConnectionDispatcher = mockConnectionDispatcher };
            var transport = new LibuvTransport(mockLibuv, transportContext, null);
            var thread = new LibuvThread(transport);
            mockConnectionDispatcher.InputOptions = pool =>
                new PipeOptions(
                    pool: pool,
                    pauseWriterThreshold: 3,
                    readerScheduler: PipeScheduler.Inline,
                    writerScheduler: PipeScheduler.Inline,
                    useSynchronizationContext: false);

            // We don't set the output writer scheduler here since we want to run the callback inline

            mockConnectionDispatcher.OutputOptions = pool => new PipeOptions(pool: pool, readerScheduler: thread, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);


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
                    var connection = new LibuvConnection(socket, listenerContext.TransportContext.Log, thread, null, null);
                    listenerContext.TransportContext.ConnectionDispatcher.OnConnection(connection);
                    connectionTask = connection.Start();

                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 5, ref ignored);

                }, null);

                // Now assert that we removed the callback from libuv to stop reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now complete the output writer so that the connection closes
                mockConnectionDispatcher.Output.Writer.Complete();

                await connectionTask.DefaultTimeout();

                // Assert that we don't try to start reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);
            }
            finally
            {
                mockConnectionDispatcher.Input.Reader.Complete();
                mockConnectionDispatcher.Output.Writer.Complete();

                await thread.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public async Task ConnectionDoesNotResumeAfterReadCallbackScheduledAndSocketCloseIfBackpressureIsApplied()
        {
            var mockConnectionDispatcher = new MockConnectionDispatcher();
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext { ConnectionDispatcher = mockConnectionDispatcher };
            var transport = new LibuvTransport(mockLibuv, transportContext, null);
            var thread = new LibuvThread(transport);
            var mockScheduler = new Mock<PipeScheduler>();
            Action backPressure = null;
            mockScheduler.Setup(m => m.Schedule(It.IsAny<Action<object>>(), It.IsAny<object>())).Callback<Action<object>, object>((a, o) =>
            {
                backPressure = () => a(o);
            });
            mockConnectionDispatcher.InputOptions = pool =>
                new PipeOptions(
                    pool: pool,
                    pauseWriterThreshold: 3,
                    resumeWriterThreshold: 3,
                    writerScheduler: mockScheduler.Object,
                    readerScheduler: PipeScheduler.Inline,
                    useSynchronizationContext: false);

            mockConnectionDispatcher.OutputOptions = pool => new PipeOptions(pool: pool, readerScheduler: thread, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);

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
                    var connection = new LibuvConnection(socket, listenerContext.TransportContext.Log, thread, null, null);
                    listenerContext.TransportContext.ConnectionDispatcher.OnConnection(connection);
                    connectionTask = connection.Start();

                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 5, ref ignored);

                }, null);

                // Now assert that we removed the callback from libuv to stop reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now release backpressure by reading the input
                var result = await mockConnectionDispatcher.Input.Reader.ReadAsync();
                // Calling advance will call into our custom scheduler that captures the back pressure
                // callback
                mockConnectionDispatcher.Input.Reader.AdvanceTo(result.Buffer.End);

                // Cancel the current pending flush
                mockConnectionDispatcher.Input.Writer.CancelPendingFlush();

                // Now release the back pressure
                await thread.PostAsync(a => a(), backPressure);

                // Assert that we don't try to start reading since the write was cancelled
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now complete the output writer and wait for the connection to close
                mockConnectionDispatcher.Output.Writer.Complete();

                await connectionTask.DefaultTimeout();

                // Assert that we don't try to start reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);
            }
            finally
            {
                mockConnectionDispatcher.Input.Reader.Complete();
                mockConnectionDispatcher.Output.Writer.Complete();

                await thread.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public async Task DoesNotThrowIfOnReadCallbackCalledWithEOFButAllocCallbackNotCalled()
        {
            var mockConnectionDispatcher = new MockConnectionDispatcher();
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext { ConnectionDispatcher = mockConnectionDispatcher };
            var transport = new LibuvTransport(mockLibuv, transportContext, null);
            var thread = new LibuvThread(transport);

            Task connectionTask = null;
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
                    var connection = new LibuvConnection(socket, listenerContext.TransportContext.Log, thread, null, null);
                    listenerContext.TransportContext.ConnectionDispatcher.OnConnection(connection);
                    connectionTask = connection.Start();

                    var ignored = new LibuvFunctions.uv_buf_t();
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), TestConstants.EOF, ref ignored);
                }, (object)null);

                var readAwaitable = await mockConnectionDispatcher.Input.Reader.ReadAsync();
                Assert.True(readAwaitable.IsCompleted);
            }
            finally
            {
                mockConnectionDispatcher.Input.Reader.Complete();
                mockConnectionDispatcher.Output.Writer.Complete();
                await connectionTask;

                await thread.StopAsync(TimeSpan.FromSeconds(5));
            }
        }
    }
}