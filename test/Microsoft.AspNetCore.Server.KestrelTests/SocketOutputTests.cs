// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class SocketOutputTests
    {
        public static TheoryData<KestrelServerOptions> MaxResponseBufferSizeData => new TheoryData<KestrelServerOptions>
        {
            new KestrelServerOptions(),
            new KestrelServerOptions
            {
                Limits = { MaxResponseBufferSize = 0 }
            },
            new KestrelServerOptions
            {
                Limits = { MaxResponseBufferSize = 1024 }
            },
            new KestrelServerOptions
            {
                Limits = { MaxResponseBufferSize = 1024 * 1024 }
            },
            new KestrelServerOptions
            {
                Limits = { MaxResponseBufferSize = null }
            },
        };

        public static TheoryData<KestrelServerOptions> PositiveMaxResponseBufferSizeData => new TheoryData<KestrelServerOptions>
        {
            new KestrelServerOptions(),
            new KestrelServerOptions
            {
                Limits = { MaxResponseBufferSize = 1024 }
            },
            new KestrelServerOptions
            {
                Limits = { MaxResponseBufferSize = 1024 * 1024 }
            }
        };

        [Theory]
        [MemberData(nameof(MaxResponseBufferSizeData))]
        public async Task CanWrite1MB(KestrelServerOptions options)
        {
            // This test was added because when initially implementing write-behind buffering in
            // SocketOutput, the write callback would never be invoked for writes larger than
            // _maxBytesPreCompleted even after the write actually completed.

            // Arrange
            var mockLibuv = new MockLibuv();
            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var socketOutput = new SocketOutput(kestrelThread, socket, new MockConnection(options), "0", trace, ltp);

                // At least one run of this test should have a MaxResponseBufferSize < 1 MB.
                var bufferSize = 1024 * 1024;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act
                var writeTask = socketOutput.WriteAsync(buffer, default(CancellationToken));
                await mockLibuv.OnPostTask;

                // Assert
                Assert.Equal(TaskStatus.RanToCompletion, writeTask.Status);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);
            }
        }

        [Fact]
        public async Task NullMaxResponseBufferSizeAllowsUnlimitedBuffer()
        {
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
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var options = new KestrelServerOptions { Limits = { MaxResponseBufferSize = null } };
                var socketOutput = new SocketOutput(kestrelThread, socket, new MockConnection(options), "0", trace, ltp);

                // Don't want to allocate anything too huge for perf. This is at least larger than the default buffer.
                var bufferSize = 1024 * 1024;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act
                var writeTask = socketOutput.WriteAsync(buffer, default(CancellationToken));

                // Assert
                Assert.Equal(TaskStatus.RanToCompletion, writeTask.Status);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await mockLibuv.OnPostTask;

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Fact]
        public async Task ZeroMaxResponseBufferSizeDisablesBuffering()
        {
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
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var options = new KestrelServerOptions { Limits = { MaxResponseBufferSize = 0 } };
                var socketOutput = new SocketOutput(kestrelThread, socket, new MockConnection(options), "0", trace, ltp);

                var bufferSize = 1;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act
                var writeTask = socketOutput.WriteAsync(buffer, default(CancellationToken));

                // Assert
                Assert.False(writeTask.IsCompleted);

                // Act
                await mockLibuv.OnPostTask;

                // Finishing the write should allow the task to complete.
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);

                // Assert
                Assert.Equal(TaskStatus.RanToCompletion, writeTask.Status);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await mockLibuv.OnPostTask;

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task WritesDontCompleteImmediatelyWhenTooManyBytesAreAlreadyBuffered(KestrelServerOptions options)
        {
            var maxBytesPreCompleted = (int)options.Limits.MaxResponseBufferSize.Value;
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
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var mockConnection = new MockConnection(options);
                var socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

                var bufferSize = maxBytesPreCompleted;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act 
                var writeTask1 = socketOutput.WriteAsync(buffer, default(CancellationToken));

                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.Equal(TaskStatus.RanToCompletion, writeTask1.Status);

                // Act
                var writeTask2 = socketOutput.WriteAsync(buffer, default(CancellationToken));
                await mockLibuv.OnPostTask;

                // Assert 
                // Too many bytes are already pre-completed for the second write to pre-complete.
                Assert.False(writeTask2.IsCompleted);

                // Act
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);

                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.Equal(TaskStatus.RanToCompletion, writeTask2.Status);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await mockLibuv.OnPostTask;

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task WritesDontCompleteImmediatelyWhenTooManyBytesIncludingNonImmediateAreAlreadyBuffered(KestrelServerOptions options)
        {
            var maxBytesPreCompleted = (int)options.Limits.MaxResponseBufferSize.Value;
            var completeQueue = new ConcurrentQueue<Action<int>>();
            var writeRequested = false;

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    writeRequested = true;
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var mockConnection = new MockConnection(options);
                var socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

                var bufferSize = maxBytesPreCompleted / 2;
                var data = new byte[bufferSize];
                var halfWriteBehindBuffer = new ArraySegment<byte>(data, 0, bufferSize);

                // Act 
                var writeTask1 = socketOutput.WriteAsync(halfWriteBehindBuffer, default(CancellationToken));

                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.Equal(TaskStatus.RanToCompletion, writeTask1.Status);
                await mockLibuv.OnPostTask;
                Assert.True(writeRequested);
                writeRequested = false;

                // Add more bytes to the write-behind buffer to prevent the next write from
                var iter = socketOutput.ProducingStart();
                iter.CopyFrom(halfWriteBehindBuffer);
                socketOutput.ProducingComplete(iter);

                // Act
                var writeTask2 = socketOutput.WriteAsync(halfWriteBehindBuffer, default(CancellationToken));

                // Assert 
                // Too many bytes are already pre-completed for the fourth write to pre-complete.
                await mockLibuv.OnPostTask;
                Assert.True(writeRequested);
                Assert.False(writeTask2.IsCompleted);

                // 2 calls have been made to uv_write
                Assert.Equal(2, completeQueue.Count);

                // Act
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);

                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.Equal(TaskStatus.RanToCompletion, writeTask2.Status);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await mockLibuv.OnPostTask;

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task OnlyWritesRequestingCancellationAreErroredOnCancellation(KestrelServerOptions options)
        {
            var maxBytesPreCompleted = (int)options.Limits.MaxResponseBufferSize.Value;
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
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();

                using (var mockConnection = new MockConnection(options))
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
                    await mockLibuv.OnPostTask;

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
                    await mockLibuv.OnPostTask;

                    foreach (var triggerCompleted in completeQueue)
                    {
                        triggerCompleted(0);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task FailedWriteCompletesOrCancelsAllPendingTasks(KestrelServerOptions options)
        {
            var maxBytesPreCompleted = (int)options.Limits.MaxResponseBufferSize.Value;
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
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();

                using (var mockConnection = new MockConnection(options))
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
                    await mockLibuv.OnPostTask;

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
                    await mockLibuv.OnPostTask;

                    foreach (var triggerCompleted in completeQueue)
                    {
                        triggerCompleted(0);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task WritesDontGetCompletedTooQuickly(KestrelServerOptions options)
        {
            var maxBytesPreCompleted = (int)options.Limits.MaxResponseBufferSize.Value;
            var completeQueue = new ConcurrentQueue<Action<int>>();
            var writeCalled = false;

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    writeCalled = true;

                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var mockConnection = new MockConnection(options);
                var socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

                var bufferSize = maxBytesPreCompleted;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act (Pre-complete the maximum number of bytes in preparation for the rest of the test)
                var writeTask1 = socketOutput.WriteAsync(buffer, default(CancellationToken));

                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                await mockLibuv.OnPostTask;
                Assert.Equal(TaskStatus.RanToCompletion, writeTask1.Status);
                Assert.True(writeCalled);
                // Arrange
                writeCalled = false;

                // Act
                var writeTask2 = socketOutput.WriteAsync(buffer, default(CancellationToken));
                var writeTask3 = socketOutput.WriteAsync(buffer, default(CancellationToken));

                await mockLibuv.OnPostTask;
                Assert.True(writeCalled);
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);

                // Assert 
                // Too many bytes are already pre-completed for the third but not the second write to pre-complete.
                // https://github.com/aspnet/KestrelHttpServer/issues/356
                Assert.Equal(TaskStatus.RanToCompletion, writeTask2.Status);
                Assert.False(writeTask3.IsCompleted);

                // Act
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);

                // Assert
                // Finishing the first write should allow the third write to pre-complete.
                Assert.Equal(TaskStatus.RanToCompletion, writeTask3.Status);

                // Cleanup
                var cleanupTask = ((SocketOutput)socketOutput).WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await mockLibuv.OnPostTask;

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Theory]
        [MemberData(nameof(MaxResponseBufferSizeData))]
        public async Task ProducingStartAndProducingCompleteCanBeUsedDirectly(KestrelServerOptions options)
        {
            int nBuffers = 0;

            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    nBuffers = buffers;
                    triggerCompleted(0);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var socketOutput = new SocketOutput(kestrelThread, socket, new MockConnection(options), "0", trace, ltp);

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
                var ignore = socketOutput.WriteAsync(default(ArraySegment<byte>), default(CancellationToken));

                await mockLibuv.OnPostTask;
                Assert.Equal(2, nBuffers);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);
            }
        }

        [Theory]
        [MemberData(nameof(MaxResponseBufferSizeData))]
        public async Task OnlyAllowsUpToThreeConcurrentWrites(KestrelServerOptions options)
        {
            var writeCalled = false;
            var completeQueue = new ConcurrentQueue<Action<int>>();

            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    writeCalled = true;
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var mockConnection = new MockConnection(options);
                var socketOutput = new SocketOutput(kestrelThread, socket, mockConnection, "0", trace, ltp);

                var buffer = new ArraySegment<byte>(new byte[1]);

                // First three writes trigger uv_write
                var ignore = socketOutput.WriteAsync(buffer, CancellationToken.None);
                await mockLibuv.OnPostTask;
                Assert.True(writeCalled);
                writeCalled = false;
                ignore = socketOutput.WriteAsync(buffer, CancellationToken.None);
                await mockLibuv.OnPostTask;
                Assert.True(writeCalled);
                writeCalled = false;
                ignore = socketOutput.WriteAsync(buffer, CancellationToken.None);
                await mockLibuv.OnPostTask;
                Assert.True(writeCalled);
                writeCalled = false;

                // The fourth write won't trigger uv_write since the first three haven't completed
                ignore = socketOutput.WriteAsync(buffer, CancellationToken.None);
                await mockLibuv.OnPostTask;
                Assert.False(writeCalled);

                // Complete 1st write allowing uv_write to be triggered again
                Action<int> triggerNextCompleted;
                Assert.True(completeQueue.TryDequeue(out triggerNextCompleted));
                triggerNextCompleted(0);
                await  mockLibuv.OnPostTask;
                Assert.True(writeCalled);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await mockLibuv.OnPostTask;

                foreach (var triggerCompleted in completeQueue)
                {
                    triggerCompleted(0);
                }
            }
        }

        [Theory]
        [MemberData(nameof(MaxResponseBufferSizeData))]
        public async Task WritesAreAggregated(KestrelServerOptions options)
        {
            var writeCalled = false;
            var writeCount = 0;

            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    writeCount++;
                    triggerCompleted(0);
                    writeCalled = true;
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var socketOutput = new SocketOutput(kestrelThread, socket, new MockConnection(new KestrelServerOptions()), "0", trace, ltp);

                mockLibuv.KestrelThreadBlocker.Reset();

                var buffer = new ArraySegment<byte>(new byte[1]);

                // Two calls to WriteAsync trigger uv_write once if both calls
                // are made before write is scheduled
                var ignore = socketOutput.WriteAsync(buffer, CancellationToken.None);
                ignore = socketOutput.WriteAsync(buffer, CancellationToken.None);

                mockLibuv.KestrelThreadBlocker.Set();

                await mockLibuv.OnPostTask;

                Assert.True(writeCalled);
                writeCalled = false;

                // Write isn't called twice after the thread is unblocked
                await mockLibuv.OnPostTask;
                Assert.False(writeCalled);
                // One call to ScheduleWrite
                Assert.Equal(1, mockLibuv.PostCount);
                // One call to uv_write
                Assert.Equal(1, writeCount);

                // Cleanup
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);
            }
        }

        [Fact]
        public async Task ProducingStartAndProducingCompleteCanBeCalledAfterConnectionClose()
        {
            var mockLibuv = new MockLibuv();

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                var kestrelThread = new KestrelThread(kestrelEngine, maxLoops: 1);
                kestrelEngine.Threads.Add(kestrelThread);
                await kestrelThread.StartAsync();

                var socket = new MockSocket(mockLibuv, kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var ltp = new SynchronousThreadPool();
                var connection = new MockConnection(new KestrelServerOptions());
                var socketOutput = new SocketOutput(kestrelThread, socket, connection, "0", trace, ltp);

                // Close SocketOutput
                var cleanupTask = socketOutput.WriteAsync(
                    default(ArraySegment<byte>), default(CancellationToken), socketDisconnect: true);

                await mockLibuv.OnPostTask;

                Assert.Equal(TaskStatus.RanToCompletion, connection.SocketClosed.Status);

                var start = socketOutput.ProducingStart();

                Assert.True(start.IsDefault);
                // ProducingComplete should not throw given a default iterator
                socketOutput.ProducingComplete(start);
            }
        }
    }
}
