// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.AspNet.Server.KestrelTests.TestHelpers;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class SocketOutputTests
    {
        [Fact]
        public void CanWrite1MB()
        {
            // This test was added because when initially implementing write-behind buffering in
            // SocketOutput, the write callback would never be invoked for writes larger than
            // _maxBytesPreCompleted even after the write actually completed.

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    triggerCompleted(0);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            using (var memory = new MemoryPool2())
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, null, 0, trace, ltp, new Queue<UvWriteReq>());

                // I doubt _maxBytesPreCompleted will ever be over a MB. If it is, we should change this test.
                var bufferSize = 1048576;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
                var completedWh = new ManualResetEventSlim();

                // Act
                socketOutput.WriteAsync(buffer).ContinueWith(
                    (t) =>
                    {
                        Assert.Null(t.Exception);
                        completedWh.Set();
                    }
                );

                // Assert
                Assert.True(completedWh.Wait(1000));
            }
        }

        [Fact]
        public void WritesDontCompleteImmediatelyWhenTooManyBytesAreAlreadyPreCompleted()
        {
            // This should match _maxBytesPreCompleted in SocketOutput
            var maxBytesPreCompleted = 65536;
            var completeQueue = new Queue<Action<int>>();

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            using (var memory = new MemoryPool2())
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, null, 0, trace, ltp, new Queue<UvWriteReq>());

                var bufferSize = maxBytesPreCompleted;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
                var completedWh = new ManualResetEventSlim();
                Action<Task> onCompleted = (Task t) =>
                {
                    Assert.Null(t.Exception);
                    completedWh.Set();
                };

                // Act 
                socketOutput.WriteAsync(buffer).ContinueWith(onCompleted);
                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.True(completedWh.Wait(1000));
                // Arrange
                completedWh.Reset();
                // Act
                socketOutput.WriteAsync(buffer).ContinueWith(onCompleted);
                // Assert 
                // Too many bytes are already pre-completed for the second write to pre-complete.
                Assert.False(completedWh.Wait(1000));
                // Act
                completeQueue.Dequeue()(0);
                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(completedWh.Wait(1000));
            }
        }
        
        [Fact]
        public void WritesDontCompleteImmediatelyWhenTooManyBytesIncludingNonImmediateAreAlreadyPreCompleted()
        {
            // This should match _maxBytesPreCompleted in SocketOutput
            var maxBytesPreCompleted = 65536;
            var completeQueue = new Queue<Action<int>>();

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            using (var memory = new MemoryPool2())
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, null, 0, trace, ltp, new Queue<UvWriteReq>());

                var bufferSize = maxBytesPreCompleted;

                var data = new byte[bufferSize];
                var fullBuffer = new ArraySegment<byte>(data, 0, bufferSize);
                var halfBuffer = new ArraySegment<byte>(data, 0, bufferSize / 2);

                var completedWh = new ManualResetEventSlim();
                Action<Task> onCompleted = (Task t) =>
                {
                    Assert.Null(t.Exception);
                    completedWh.Set();
                };

                // Act 
                socketOutput.WriteAsync(halfBuffer, false).ContinueWith(onCompleted);
                // Assert
                // The first write should pre-complete since it is not immediate.
                Assert.True(completedWh.Wait(1000));
                // Arrange
                completedWh.Reset();
                // Act 
                socketOutput.WriteAsync(halfBuffer).ContinueWith(onCompleted);
                // Assert
                // The second write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.True(completedWh.Wait(1000));
                // Arrange
                completedWh.Reset();
                // Act 
                socketOutput.WriteAsync(halfBuffer, false).ContinueWith(onCompleted);
                // Assert
                // The third write should pre-complete since it is not immediate, even though too many.
                Assert.True(completedWh.Wait(1000));
                // Arrange
                completedWh.Reset();
                // Act
                socketOutput.WriteAsync(halfBuffer).ContinueWith(onCompleted);
                // Assert 
                // Too many bytes are already pre-completed for the fourth write to pre-complete.
                Assert.False(completedWh.Wait(1000));
                // Act
                while (completeQueue.Count > 0)
                {
                    completeQueue.Dequeue()(0);
                }
                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(completedWh.Wait(1000));
            }
        }

        [Fact]
        public void WritesDontGetCompletedTooQuickly()
        {
            // This should match _maxBytesPreCompleted in SocketOutput
            var maxBytesPreCompleted = 65536;
            var completeQueue = new Queue<Action<int>>();
            var onWriteWh = new ManualResetEventSlim();

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    onWriteWh.Set();

                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            using (var memory = new MemoryPool2())
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, null, 0, trace, ltp, new Queue<UvWriteReq>());

                var bufferSize = maxBytesPreCompleted;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                var completedWh = new ManualResetEventSlim();
                Action<Task> onCompleted = (Task t) =>
                {
                    Assert.Null(t.Exception);
                    completedWh.Set();
                };

                var completedWh2 = new ManualResetEventSlim();
                Action<Task> onCompleted2 = (Task t) =>
                {
                    Assert.Null(t.Exception);
                    completedWh2.Set();
                };

                // Act (Pre-complete the maximum number of bytes in preparation for the rest of the test)
                socketOutput.WriteAsync(buffer).ContinueWith(onCompleted);
                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.True(completedWh.Wait(1000));
                Assert.True(onWriteWh.Wait(1000));
                // Arrange
                completedWh.Reset();
                onWriteWh.Reset();

                // Act
                socketOutput.WriteAsync(buffer).ContinueWith(onCompleted);
                socketOutput.WriteAsync(buffer).ContinueWith(onCompleted2);

                Assert.True(onWriteWh.Wait(1000));
                completeQueue.Dequeue()(0);

                // Assert 
                // Too many bytes are already pre-completed for the third but not the second write to pre-complete.
                // https://github.com/aspnet/KestrelHttpServer/issues/356
                Assert.True(completedWh.Wait(1000));
                Assert.False(completedWh2.Wait(1000));

                // Act
                completeQueue.Dequeue()(0);

                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(completedWh2.Wait(1000));
            }
        }

        [Fact]
        public void ProducingStartAndProducingCompleteCanBeUsedDirectly()
        {
            int nBuffers = 0;
            var nBufferWh = new ManualResetEventSlim();

            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    nBuffers = buffers;
                    nBufferWh.Set();
                    triggerCompleted(0);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            using (var memory = new MemoryPool2())
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, null, 0, trace, ltp, new Queue<UvWriteReq>());

                // block 1
                var start = socketOutput.ProducingStart();
                start.Block.End = start.Block.BlockEndOffset;

                // block 2
                var block2 = memory.Lease();
                block2.End = block2.BlockEndOffset;
                start.Block.Next = block2;

                var end = new MemoryPoolIterator2(block2, block2.End);

                socketOutput.ProducingComplete(end);

                // A call to Write is required to ensure a write is scheduled
                socketOutput.WriteAsync(default(ArraySegment<byte>));

                Assert.True(nBufferWh.Wait(1000));
                Assert.Equal(2, nBuffers);
            }
        }


        private class MockSocket : UvStreamHandle
        {
            public MockSocket(int threadId, IKestrelTrace logger) : base(logger)
            {
                // Set the handle to something other than IntPtr.Zero
                // so handle.Validate doesn't fail in Libuv.write
                handle = (IntPtr)1;
                _threadId = threadId;
            }

            protected override bool ReleaseHandle()
            {
                // No-op
                return true;
            }
        }
    }
}
