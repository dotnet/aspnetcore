// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
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

            using (var memory = new MemoryPool2())
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, new MockConnection(), 0, trace, ltp, new Queue<UvWriteReq>());

                // I doubt _maxBytesPreCompleted will ever be over a MB. If it is, we should change this test.
                var bufferSize = 1048576;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
                var completedWh = new ManualResetEventSlim();

                // Act
                socketOutput.WriteAsync(buffer, default(CancellationToken)).ContinueWith(
                    (t) =>
                    {
                        Assert.Null(t.Exception);
                        completedWh.Set();
                    }
                );

                // Assert
                Assert.True(completedWh.Wait(1000));

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);
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

            using (var memory = new MemoryPool2())
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, new MockConnection(), 0, trace, ltp, new Queue<UvWriteReq>());

                var bufferSize = maxBytesPreCompleted;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
                var completedWh = new ManualResetEventSlim();
                Action<Task> onCompleted = (Task t) =>
                {
                    Assert.Null(t.Exception);
                    completedWh.Set();
                };

                // Act 
                socketOutput.WriteAsync(buffer, default(CancellationToken)).ContinueWith(onCompleted);
                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.True(completedWh.Wait(1000));
                // Arrange
                completedWh.Reset();
                // Act
                socketOutput.WriteAsync(buffer, default(CancellationToken)).ContinueWith(onCompleted);
                // Assert 
                // Too many bytes are already pre-completed for the second write to pre-complete.
                Assert.False(completedWh.Wait(1000));
                // Act
                completeQueue.Dequeue()(0);
                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(completedWh.Wait(1000));

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Fact]
        public void WritesDontCompleteImmediatelyWhenTooManyBytesIncludingNonImmediateAreAlreadyPreCompleted()
        {
            // This should match _maxBytesPreCompleted in SocketOutput
            var maxBytesPreCompleted = 65536;
            var completeQueue = new Queue<Action<int>>();
            var writeRequestedWh = new ManualResetEventSlim();

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    writeRequestedWh.Set();
                    return 0;
                }
            };

            using (var memory = new MemoryPool2())
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, new MockConnection(), 0, trace, ltp, new Queue<UvWriteReq>());

                var bufferSize = maxBytesPreCompleted / 2;
                var data = new byte[bufferSize];
                var halfWriteBehindBuffer = new ArraySegment<byte>(data, 0, bufferSize);

                // Act 
                var writeTask1 = socketOutput.WriteAsync(halfWriteBehindBuffer, default(CancellationToken));
                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.Equal(TaskStatus.RanToCompletion, writeTask1.Status);
                Assert.True(writeRequestedWh.Wait(1000));
                writeRequestedWh.Reset();

                // Add more bytes to the write-behind buffer to prevent the next write from
                var iter = socketOutput.ProducingStart();
                iter.CopyFrom(halfWriteBehindBuffer);
                socketOutput.ProducingComplete(iter);

                // Act
                var writeTask2 = socketOutput.WriteAsync(halfWriteBehindBuffer, default(CancellationToken));
                // Assert 
                // Too many bytes are already pre-completed for the fourth write to pre-complete.
                Assert.True(writeRequestedWh.Wait(1000));
                Assert.False(writeTask2.IsCompleted);

                // 2 calls have been made to uv_write
                Assert.Equal(2, completeQueue.Count);

                // Act
                completeQueue.Dequeue()(0);

                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(writeTask2.Wait(1000));

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Fact]
        public async Task OnlyWritesRequestingCancellationAreErroredOnCancellation()
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

            using (var memory = new MemoryPool2())
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                
                using (var mockConnection = new MockConnection())
                {
                    ISocketOutput socketOutput = new SocketOutput(kestrelThread, socket, memory, mockConnection, 0, trace, ltp, new Queue<UvWriteReq>());

                    var bufferSize = maxBytesPreCompleted;

                    var data = new byte[bufferSize];
                    var fullBuffer = new ArraySegment<byte>(data, 0, bufferSize);

                    var cts = new CancellationTokenSource();

                    // Act 
                    var task1Success = socketOutput.WriteAsync(fullBuffer, cancellationToken: cts.Token);
                    // task1 should complete successfully as < _maxBytesPreCompleted

                    // First task is completed and successful
                    Assert.True(task1Success.IsCompleted);
                    Assert.False(task1Success.IsCanceled);
                    Assert.False(task1Success.IsFaulted);

                    // following tasks should wait.
                    var task2Throw = socketOutput.WriteAsync(fullBuffer, cancellationToken: cts.Token);
                    var task3Success = socketOutput.WriteAsync(fullBuffer, cancellationToken: default(CancellationToken));

                    // Give time for tasks to percolate
                    await Task.Delay(1000);

                    // Second task is not completed
                    Assert.False(task2Throw.IsCompleted);
                    Assert.False(task2Throw.IsCanceled);
                    Assert.False(task2Throw.IsFaulted);

                    // Third task is not completed 
                    Assert.False(task3Success.IsCompleted);
                    Assert.False(task3Success.IsCanceled);
                    Assert.False(task3Success.IsFaulted);

                    cts.Cancel();

                    // Second task is now canceled
                    await Assert.ThrowsAsync<TaskCanceledException>(() => task2Throw);
                    Assert.True(task2Throw.IsCanceled);

                    // Third task is now completed
                    await task3Success;

                    // Fourth task immediately cancels as the token is canceled 
                    var task4Throw = socketOutput.WriteAsync(fullBuffer, cancellationToken: cts.Token);

                    Assert.True(task4Throw.IsCompleted);
                    Assert.True(task4Throw.IsCanceled);
                    Assert.False(task4Throw.IsFaulted);

                    var task5Success = socketOutput.WriteAsync(fullBuffer, cancellationToken: default(CancellationToken));
                    // task5 should complete immediately

                    Assert.True(task5Success.IsCompleted);
                    Assert.False(task5Success.IsCanceled);
                    Assert.False(task5Success.IsFaulted);

                    cts = new CancellationTokenSource();

                    var task6Success = socketOutput.WriteAsync(fullBuffer, cancellationToken: cts.Token);
                    // task6 should complete immediately but not cancel as its cancellation token isn't set

                    Assert.True(task6Success.IsCompleted);
                    Assert.False(task6Success.IsCanceled);
                    Assert.False(task6Success.IsFaulted);

                    Assert.True(true);

                    // Cleanup
                    var cleanupTask = ((SocketOutput)socketOutput).WriteAsync(
                        default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                    foreach (var triggerCompleted in completeQueue)
                    {
                        triggerCompleted(0);
                    }
                }
            }
        }

        [Fact]
        public async Task FailedWriteCompletesOrCancelsAllPendingTasks()
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

            using (var memory = new MemoryPool2())
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);

                using (var mockConnection = new MockConnection())
                {
                    var abortedSource = mockConnection.RequestAbortedSource;
                    ISocketOutput socketOutput = new SocketOutput(kestrelThread, socket, memory, mockConnection, 0, trace, ltp, new Queue<UvWriteReq>());

                    var bufferSize = maxBytesPreCompleted;

                    var data = new byte[bufferSize];
                    var fullBuffer = new ArraySegment<byte>(data, 0, bufferSize);

                    // Act 
                    var task1Success = socketOutput.WriteAsync(fullBuffer, cancellationToken: abortedSource.Token);
                    // task1 should complete successfully as < _maxBytesPreCompleted

                    // First task is completed and successful
                    Assert.True(task1Success.IsCompleted);
                    Assert.False(task1Success.IsCanceled);
                    Assert.False(task1Success.IsFaulted);

                    // following tasks should wait.
                    var task2Success = socketOutput.WriteAsync(fullBuffer, cancellationToken: default(CancellationToken));
                    var task3Canceled = socketOutput.WriteAsync(fullBuffer, cancellationToken: abortedSource.Token);

                    // Give time for tasks to percolate
                    await Task.Delay(1000);

                    // Second task is not completed
                    Assert.False(task2Success.IsCompleted);
                    Assert.False(task2Success.IsCanceled);
                    Assert.False(task2Success.IsFaulted);

                    // Third task is not completed 
                    Assert.False(task3Canceled.IsCompleted);
                    Assert.False(task3Canceled.IsCanceled);
                    Assert.False(task3Canceled.IsFaulted);

                    // Cause the first write to fail.
                    completeQueue.Dequeue()(-1);

                    // Second task is now completed
                    await task2Success;

                    // Third task is now canceled
                    await Assert.ThrowsAsync<TaskCanceledException>(() => task3Canceled);
                    Assert.True(task3Canceled.IsCanceled);

                    // Cleanup
                    var cleanupTask = ((SocketOutput)socketOutput).WriteAsync(
                        default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                    foreach (var triggerCompleted in completeQueue)
                    {
                        triggerCompleted(0);
                    }
                }
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

            using (var memory = new MemoryPool2())
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, new MockConnection(), 0, trace, ltp, new Queue<UvWriteReq>());

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
                socketOutput.WriteAsync(buffer, default(CancellationToken)).ContinueWith(onCompleted);
                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.True(completedWh.Wait(1000));
                Assert.True(onWriteWh.Wait(1000));
                // Arrange
                completedWh.Reset();
                onWriteWh.Reset();

                // Act
                socketOutput.WriteAsync(buffer, default(CancellationToken)).ContinueWith(onCompleted);
                socketOutput.WriteAsync(buffer, default(CancellationToken)).ContinueWith(onCompleted2);

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

                // Cleanup
                var cleanupTask = ((SocketOutput)socketOutput).WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
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

            using (var memory = new MemoryPool2())
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, memory, new MockConnection(), 0, trace, ltp, new Queue<UvWriteReq>());

                // block 1
                var start = socketOutput.ProducingStart();
                start.Block.End = start.Block.Data.Offset + start.Block.Data.Count;

                // block 2
                var block2 = memory.Lease();
                block2.End = block2.Data.Offset + block2.Data.Count;
                start.Block.Next = block2;

                var end = new MemoryPoolIterator2(block2, block2.End);

                socketOutput.ProducingComplete(end);

                // A call to Write is required to ensure a write is scheduled
                socketOutput.WriteAsync(default(ArraySegment<byte>), default(CancellationToken));

                Assert.True(nBufferWh.Wait(1000));
                Assert.Equal(2, nBuffers);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);
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
