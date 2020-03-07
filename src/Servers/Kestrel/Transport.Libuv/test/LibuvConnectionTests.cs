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
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext();
            var thread = new LibuvThread(mockLibuv, transportContext);
            var listenerContext = new ListenerContext(transportContext)
            {
                Thread = thread
            };

            try
            {
                await thread.StartAsync();
                await thread.PostAsync(_ =>
                {      
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, transportContext.Log);
                    listenerContext.HandleConnection(socket);

                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 0, ref ignored);
                }, (object)null);

                await using var connection = await listenerContext.AcceptAsync();

                var readAwaitable = connection.Transport.Input.ReadAsync();
                Assert.False(readAwaitable.IsCompleted);
            }
            finally
            {
                await thread.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public async Task ConnectionDoesNotResumeAfterSocketCloseIfBackpressureIsApplied()
        {
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext();
            var thread = new LibuvThread(mockLibuv, transportContext);
            var listenerContext = new ListenerContext(transportContext)
            {
                Thread = thread,
                InputOptions = new PipeOptions(
                    pool: thread.MemoryPool,
                    pauseWriterThreshold: 3,
                    readerScheduler: PipeScheduler.Inline,
                    writerScheduler: PipeScheduler.Inline,
                    useSynchronizationContext: false),

                // We don't set the output writer scheduler here since we want to run the callback inline
                OutputOptions = new PipeOptions(
                    pool: thread.MemoryPool,
                    readerScheduler: thread,
                    writerScheduler: PipeScheduler.Inline,
                    useSynchronizationContext: false)
            };

            try
            {
                await thread.StartAsync();

                // Write enough to make sure back pressure will be applied
                await thread.PostAsync<object>(_ =>
                {
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, transportContext.Log);
                    listenerContext.HandleConnection(socket);

                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 5, ref ignored);

                }, null);

                var connection = await listenerContext.AcceptAsync();

                // Now assert that we removed the callback from libuv to stop reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now complete the output writer so that the connection closes
                await connection.DisposeAsync();

                // Assert that we don't try to start reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);
            }
            finally
            {
                await thread.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public async Task ConnectionDoesNotResumeAfterReadCallbackScheduledAndSocketCloseIfBackpressureIsApplied()
        {
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext();
            var thread = new LibuvThread(mockLibuv, transportContext);
            var mockScheduler = new Mock<PipeScheduler>();
            Action backPressure = null;
            mockScheduler.Setup(m => m.Schedule(It.IsAny<Action<object>>(), It.IsAny<object>())).Callback<Action<object>, object>((a, o) =>
            {
                backPressure = () => a(o);
            });
            var listenerContext = new ListenerContext(transportContext)
            {
                Thread = thread,
                InputOptions = new PipeOptions(
                    pool: thread.MemoryPool,
                    pauseWriterThreshold: 3,
                    resumeWriterThreshold: 3,
                    writerScheduler: mockScheduler.Object,
                    readerScheduler: PipeScheduler.Inline,
                    useSynchronizationContext: false),
                OutputOptions = new PipeOptions(
                    pool: thread.MemoryPool,
                    readerScheduler: thread,
                    writerScheduler: PipeScheduler.Inline,
                    useSynchronizationContext: false)
            };

            try
            {
                await thread.StartAsync();

                // Write enough to make sure back pressure will be applied
                await thread.PostAsync<object>(_ =>
                {
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, transportContext.Log);
                    listenerContext.HandleConnection(socket);
                    
                    mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out var ignored);
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), 5, ref ignored);

                }, null);

                var connection = await listenerContext.AcceptAsync();

                // Now assert that we removed the callback from libuv to stop reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now release backpressure by reading the input
                var result = await connection.Transport.Input.ReadAsync();
                // Calling advance will call into our custom scheduler that captures the back pressure
                // callback
                connection.Transport.Input.AdvanceTo(result.Buffer.End);

                // Cancel the current pending flush
                connection.Application.Output.CancelPendingFlush();

                // Now release the back pressure
                await thread.PostAsync(a => a(), backPressure);

                // Assert that we don't try to start reading since the write was cancelled
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);

                // Now complete the output writer and wait for the connection to close
                await connection.DisposeAsync();
                
                // Assert that we don't try to start reading
                Assert.Null(mockLibuv.AllocCallback);
                Assert.Null(mockLibuv.ReadCallback);
            }
            finally
            {
                await thread.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        [Fact]
        public async Task DoesNotThrowIfOnReadCallbackCalledWithEOFButAllocCallbackNotCalled()
        {
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext();
            var thread = new LibuvThread(mockLibuv, transportContext);
            var listenerContext = new ListenerContext(transportContext)
            {
                Thread = thread
            };

            try
            {
                await thread.StartAsync();
                await thread.PostAsync(_ =>
                {
                    var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, transportContext.Log);
                    listenerContext.HandleConnection(socket);
                    
                    var ignored = new LibuvFunctions.uv_buf_t();
                    mockLibuv.ReadCallback(socket.InternalGetHandle(), TestConstants.EOF, ref ignored);
                }, (object)null);

                await using var connection = await listenerContext.AcceptAsync();

                var readAwaitable = await connection.Transport.Input.ReadAsync();
                Assert.True(readAwaitable.IsCompleted);
            }
            finally
            {
                await thread.StopAsync(TimeSpan.FromSeconds(5));
            }
        }
    }
}
