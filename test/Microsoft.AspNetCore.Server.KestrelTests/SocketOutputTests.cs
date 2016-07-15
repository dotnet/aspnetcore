// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
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
            var mockLibuv = new MockLibuv();
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, new MockConnection(), "0", trace, ltp);

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
            var completeQueue = new ConcurrentQueue<Action<int>>();

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
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var mockConnection = new MockConnection();
                var socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

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
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);
                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(completedWh.Wait(1000));

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                Assert.True(mockConnection.SocketClosed.Wait(1000));

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
            var completeQueue = new ConcurrentQueue<Action<int>>();
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

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var mockConnection = new MockConnection();
                var socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

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
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);

                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(writeTask2.Wait(1000));

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                Assert.True(mockConnection.SocketClosed.Wait(1000));

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
            var completeQueue = new ConcurrentQueue<Action<int>>();

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
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);

                using (var mockConnection = new MockConnection())
                {
                    ISocketOutput socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

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

                    // Cleanup
                    var cleanupTask = ((SocketOutput)socketOutput).WriteAsync(
                        default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                    // Allow for the socketDisconnect command to get posted to the libuv thread.
                    // Right now, the up to three pending writes are holding it up.
                    Action<int> triggerNextCompleted;
                    Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                    triggerNextCompleted(0);

                    // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                    Assert.True(mockConnection.SocketClosed.Wait(1000));

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
            var completeQueue = new ConcurrentQueue<Action<int>>();

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
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);

                using (var mockConnection = new MockConnection())
                {
                    var abortedSource = mockConnection.RequestAbortedSource;
                    ISocketOutput socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

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
                    Action<int> triggerNextCompleted;
                    Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                    triggerNextCompleted(-1);

                    // Second task is now completed
                    await task2Success;

                    // Third task is now canceled
                    await Assert.ThrowsAsync<TaskCanceledException>(() => task3Canceled);
                    Assert.True(task3Canceled.IsCanceled);

                    // Cleanup
                    var cleanupTask = ((SocketOutput)socketOutput).WriteAsync(
                        default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                    // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                    Assert.True(mockConnection.SocketClosed.Wait(1000));

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
            var completeQueue = new ConcurrentQueue<Action<int>>();
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
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var mockConnection = new MockConnection();
                var socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

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
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);

                // Assert 
                // Too many bytes are already pre-completed for the third but not the second write to pre-complete.
                // https://github.com/aspnet/KestrelHttpServer/issues/356
                Assert.True(completedWh.Wait(1000));
                Assert.False(completedWh2.Wait(1000));

                // Act
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);

                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(completedWh2.Wait(1000));

                // Cleanup
                var cleanupTask = ((SocketOutput)socketOutput).WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                Assert.True(mockConnection.SocketClosed.Wait(1000));

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

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, new MockConnection(), "0", trace, ltp);

                // block 1
                var start = socketOutput.ProducingStart();
                start.Block.End = start.Block.Data.Offset + start.Block.Data.Count;

                // block 2
                var block2 = kestrelThread.Memory.Lease();
                block2.End = block2.Data.Offset + block2.Data.Count;
                start.Block.Next = block2;

                var end = new MemoryPoolIterator(block2, block2.End);

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

        [Fact]
        public void OnlyAllowsUpToThreeConcurrentWrites()
        {
            var writeWh = new ManualResetEventSlim();
            var completeQueue = new ConcurrentQueue<Action<int>>();

            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    writeWh.Set();
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var mockConnection = new MockConnection();
                var socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

                var buffer = new ArraySegment<byte>(new byte[1]);

                // First three writes trigger uv_write
                socketOutput.WriteAsync(buffer, CancellationToken.None);
                Assert.True(writeWh.Wait(1000));
                writeWh.Reset();
                socketOutput.WriteAsync(buffer, CancellationToken.None);
                Assert.True(writeWh.Wait(1000));
                writeWh.Reset();
                socketOutput.WriteAsync(buffer, CancellationToken.None);
                Assert.True(writeWh.Wait(1000));
                writeWh.Reset();

                // The fourth write won't trigger uv_write since the first three haven't completed
                socketOutput.WriteAsync(buffer, CancellationToken.None);
                Assert.False(writeWh.Wait(1000));

                // Complete 1st write allowing uv_write to be triggered again
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);
                Assert.True(writeWh.Wait(1000));

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Allow for the socketDisconnect command to get posted to the libuv thread.
                // Right now, the three pending writes are holding it up.
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);
                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                Assert.True(mockConnection.SocketClosed.Wait(1000));

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Fact]
        public void WritesAreAggregated()
        {
            var writeWh = new ManualResetEventSlim();
            var writeCount = 0;

            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    writeCount++;
                    triggerCompleted(0);
                    writeWh.Set();
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var socketOutput = new SocketOutput(kestrelThread, socket, new MockConnection(), "0", trace, ltp);

                var blockThreadWh = new ManualResetEventSlim();
                kestrelThread.Post(_ =>
                {
                    blockThreadWh.Wait();
                }, state: null);

                var buffer = new ArraySegment<byte>(new byte[1]);

                // Two calls to WriteAsync trigger uv_write once if both calls
                // are made before write is scheduled
                socketOutput.WriteAsync(buffer, CancellationToken.None);
                socketOutput.WriteAsync(buffer, CancellationToken.None);

                blockThreadWh.Set();

                Assert.True(writeWh.Wait(1000));
                writeWh.Reset();

                // Write isn't called twice after the thread is unblocked
                Assert.False(writeWh.Wait(1000));
                Assert.Equal(1, writeCount);
                // One call to ScheduleWrite + One call to Post to block the thread
                Assert.Equal(2, mockLibuv.PostCount);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);
            }
        }

        [Fact]
        public void ProducingStartAndProducingCompleteCanBeCalledAfterConnectionClose()
        {
            var mockLibuv = new MockLibuv();

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new LoggingThreadPool(trace);
                var connection = new MockConnection();
                var socketOutput = new SocketOutput(kestrelThread, socket, connection, "0", trace, ltp);

                // Close SocketOutput
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                Assert.True(connection.SocketClosed.Wait(1000));

                var start = socketOutput.ProducingStart();

                Assert.True(start.IsDefault);
                // ProducingComplete should not throw given a default iterator
                socketOutput.ProducingComplete(start);
            }
        }
    }
}
