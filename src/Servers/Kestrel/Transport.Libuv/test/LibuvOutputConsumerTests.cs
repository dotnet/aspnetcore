// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class LibuvOutputConsumerTests : IDisposable
    {
        private readonly MemoryPool<byte> _memoryPool;
        private readonly MockLibuv _mockLibuv;
        private readonly LibuvThread _libuvThread;

        public static TheoryData<long?> MaxResponseBufferSizeData => new TheoryData<long?>
        {
            new KestrelServerOptions().Limits.MaxResponseBufferSize, 0, 1024, 1024 * 1024, null
        };

        public static TheoryData<int> PositiveMaxResponseBufferSizeData => new TheoryData<int>
        {
            (int)new KestrelServerOptions().Limits.MaxResponseBufferSize, 1024, (1024 * 1024) + 1
        };

        public LibuvOutputConsumerTests()
        {
            _memoryPool = SlabMemoryPoolFactory.Create();
            _mockLibuv = new MockLibuv();

            var context = new TestLibuvTransportContext();
            _libuvThread = new LibuvThread(_mockLibuv, context, maxLoops: 1);
            _libuvThread.StartAsync().Wait();
        }

        public void Dispose()
        {
            _libuvThread.StopAsync(TimeSpan.FromSeconds(5)).Wait();
            _memoryPool.Dispose();
        }

        [Theory]
        [MemberData(nameof(MaxResponseBufferSizeData))]
        public async Task CanWrite1MB(long? maxResponseBufferSize)
        {
            // This test was added because when initially implementing write-behind buffering in
            // SocketOutput, the write callback would never be invoked for writes larger than
            // maxResponseBufferSize even after the write actually completed.

            // ConnectionHandler will set Pause/ResumeWriterThreshold to zero when MaxResponseBufferSize is null.
            // This is verified in PipeOptionsTests.OutputPipeOptionsConfiguredCorrectly.
            var pipeOptions = new PipeOptions
            (
                pool: _memoryPool,
                readerScheduler: _libuvThread,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: maxResponseBufferSize ?? 0,
                resumeWriterThreshold: maxResponseBufferSize ?? 0,
                useSynchronizationContext: false
            );

            await using (var processor = CreateOutputProducer(pipeOptions))
            {
                var outputProducer = processor.OutputProducer;
                // At least one run of this test should have a MaxResponseBufferSize < 1 MB.
                var bufferSize = 1024 * 1024;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act
                var writeTask = outputProducer.WriteDataAsync(buffer);

                // Assert
                await writeTask.DefaultTimeout();
            }
        }

        [Fact]
        public async Task NullMaxResponseBufferSizeAllowsUnlimitedBuffer()
        {
            var completeQueue = new ConcurrentQueue<Action<int>>();

            // Arrange
            _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
            {
                completeQueue.Enqueue(triggerCompleted);
                return 0;
            };

            // ConnectionHandler will set Pause/ResumeWriterThreshold to zero when MaxResponseBufferSize is null.
            // This is verified in PipeOptionsTests.OutputPipeOptionsConfiguredCorrectly.
            var pipeOptions = new PipeOptions
            (
                pool: _memoryPool,
                readerScheduler: _libuvThread,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: 0,
                resumeWriterThreshold: 0,
                useSynchronizationContext: false
            );

            await using (var processor = CreateOutputProducer(pipeOptions))
            {
                var outputProducer = processor.OutputProducer;
                // Don't want to allocate anything too huge for perf. This is at least larger than the default buffer.
                var bufferSize = 1024 * 1024;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act
                var writeTask = outputProducer.WriteDataAsync(buffer);

                // Assert
                await writeTask.DefaultTimeout();

                // Cleanup
                outputProducer.Dispose();

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await _mockLibuv.OnPostTask;

                // Drain the write queue
                while (completeQueue.TryDequeue(out var triggerNextCompleted))
                {
                    await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                }
            }
        }

        [Fact]
        public async Task ZeroMaxResponseBufferSizeDisablesBuffering()
        {
            var completeQueue = new ConcurrentQueue<Action<int>>();

            // Arrange
            _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
            {
                completeQueue.Enqueue(triggerCompleted);
                return 0;
            };

            // ConnectionHandler will set Pause/ResumeWriterThreshold to 1 when MaxResponseBufferSize is zero.
            // This is verified in PipeOptionsTests.OutputPipeOptionsConfiguredCorrectly.
            var pipeOptions = new PipeOptions
            (
                pool: _memoryPool,
                readerScheduler: _libuvThread,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: 1,
                resumeWriterThreshold: 1,
                useSynchronizationContext: false
            );

            await using (var processor = CreateOutputProducer(pipeOptions))
            {
                var outputProducer = processor.OutputProducer;
                var bufferSize = 1;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act
                var writeTask = outputProducer.WriteDataAsync(buffer);

                // Assert
                Assert.False(writeTask.IsCompleted);

                // Act
                await _mockLibuv.OnPostTask;

                // Finishing the write should allow the task to complete.
                Assert.True(completeQueue.TryDequeue(out var triggerNextCompleted));
                await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);

                // Assert
                await writeTask.DefaultTimeout();

                // Cleanup
                outputProducer.Dispose();

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await _mockLibuv.OnPostTask;

                // Drain the write queue
                while (completeQueue.TryDequeue(out triggerNextCompleted))
                {
                    await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                }
            }
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task WritesDontCompleteImmediatelyWhenTooManyBytesAreAlreadyBuffered(int maxResponseBufferSize)
        {
            var completeQueue = new ConcurrentQueue<Action<int>>();

            // Arrange
            _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
            {
                completeQueue.Enqueue(triggerCompleted);
                return 0;
            };

            var pipeOptions = new PipeOptions
            (
                pool: _memoryPool,
                readerScheduler: _libuvThread,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: maxResponseBufferSize,
                resumeWriterThreshold: maxResponseBufferSize,
                useSynchronizationContext: false
            );

            await using (var processor = CreateOutputProducer(pipeOptions))
            {
                var outputProducer = processor.OutputProducer;
                var bufferSize = maxResponseBufferSize - 1;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act
                var writeTask1 = outputProducer.WriteDataAsync(buffer);

                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.Equal(TaskStatus.RanToCompletion, writeTask1.Status);

                // Act
                var writeTask2 = outputProducer.WriteDataAsync(buffer);
                await _mockLibuv.OnPostTask;

                // Assert
                // Too many bytes are already pre-completed for the second write to pre-complete.
                Assert.False(writeTask2.IsCompleted);

                // Act
                Assert.True(completeQueue.TryDequeue(out var triggerNextCompleted));
                await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);

                // Finishing the first write should allow the second write to pre-complete.
                await writeTask2.DefaultTimeout();

                // Cleanup
                outputProducer.Dispose();

                // Wait for all writes to complete so the completeQueue isn't modified during enumeration.
                await _mockLibuv.OnPostTask;

                // Drain the write queue
                while (completeQueue.TryDequeue(out triggerNextCompleted))
                {
                    await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                }
            }
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task WritesDontCompleteImmediatelyWhenTooManyBytesIncludingNonImmediateAreAlreadyBuffered(int maxResponseBufferSize)
        {
            await Task.Run(async () =>
            {
                var completeQueue = new ConcurrentQueue<Action<int>>();

                // Arrange
                _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                };

                var pipeOptions = new PipeOptions
                (
                    pool: _memoryPool,
                    readerScheduler: _libuvThread,
                    writerScheduler: PipeScheduler.Inline,
                    pauseWriterThreshold: maxResponseBufferSize,
                    resumeWriterThreshold: maxResponseBufferSize,
                    useSynchronizationContext: false
                );

                await using (var processor = CreateOutputProducer(pipeOptions))
                {
                    var outputProducer = processor.OutputProducer;
                    var bufferSize = maxResponseBufferSize / 2;
                    var data = new byte[bufferSize];
                    var halfWriteBehindBuffer = new ArraySegment<byte>(data, 0, bufferSize);

                    // Act
                    var writeTask1 = outputProducer.WriteDataAsync(halfWriteBehindBuffer);

                    // Assert
                    // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                    Assert.Equal(TaskStatus.RanToCompletion, writeTask1.Status);
                    await _mockLibuv.OnPostTask;
                    Assert.NotEmpty(completeQueue);

                    // Add more bytes to the write-behind buffer to prevent the next write from
                    _ = outputProducer.WriteDataAsync(halfWriteBehindBuffer, default);

                    // Act
                    var writeTask2 = outputProducer.WriteDataAsync(halfWriteBehindBuffer);
                    Assert.False(writeTask2.IsCompleted);

                    var writeTask3 = outputProducer.WriteDataAsync(halfWriteBehindBuffer);
                    Assert.False(writeTask3.IsCompleted);

                    // Drain the write queue
                    while (completeQueue.TryDequeue(out var triggerNextCompleted))
                    {
                        await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                    }

                    var timeout = TestConstants.DefaultTimeout;

                    await writeTask2.TimeoutAfter(timeout);
                    await writeTask3.TimeoutAfter(timeout);

                    Assert.Empty(completeQueue);
                }
            });
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task FailedWriteCompletesOrCancelsAllPendingTasks(int maxResponseBufferSize)
        {
            await Task.Run(async () =>
            {
                var completeQueue = new ConcurrentQueue<Action<int>>();

                // Arrange
                _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                };

                var abortedSource = new CancellationTokenSource();

                var pipeOptions = new PipeOptions
                (
                    pool: _memoryPool,
                    readerScheduler: _libuvThread,
                    writerScheduler: PipeScheduler.Inline,
                    pauseWriterThreshold: maxResponseBufferSize,
                    resumeWriterThreshold: maxResponseBufferSize,
                    useSynchronizationContext: false
                );

                await using (var processor = CreateOutputProducer(pipeOptions, abortedSource))
                {
                    var outputProducer = processor.OutputProducer;
                    var bufferSize = maxResponseBufferSize - 1;

                    var data = new byte[bufferSize];
                    var fullBuffer = new ArraySegment<byte>(data, 0, bufferSize);

                    // Act
                    var task1Success = outputProducer.WriteDataAsync(fullBuffer, cancellationToken: abortedSource.Token);
                    // task1 should complete successfully as < _maxBytesPreCompleted

                    // First task is completed and successful
                    Assert.True(task1Success.IsCompleted);
                    Assert.False(task1Success.IsCanceled);
                    Assert.False(task1Success.IsFaulted);

                    // following tasks should wait.
                    var task2Success = outputProducer.WriteDataAsync(fullBuffer);
                    var task3Canceled = outputProducer.WriteDataAsync(fullBuffer, cancellationToken: abortedSource.Token);

                    // Give time for tasks to percolate
                    await _mockLibuv.OnPostTask;

                    // Second task is not completed
                    Assert.False(task2Success.IsCompleted);
                    Assert.False(task2Success.IsCanceled);
                    Assert.False(task2Success.IsFaulted);

                    // Third task is not completed
                    Assert.False(task3Canceled.IsCompleted);
                    Assert.False(task3Canceled.IsCanceled);
                    Assert.False(task3Canceled.IsFaulted);

                    // Cause all writes to fail
                    while (completeQueue.TryDequeue(out var triggerNextCompleted))
                    {
                        await _libuvThread.PostAsync(cb => cb(LibuvConstants.ECONNRESET.Value), triggerNextCompleted);
                    }

                    await task2Success.DefaultTimeout();

                    // Second task is now completed
                    Assert.True(task2Success.IsCompleted);
                    Assert.False(task2Success.IsCanceled);
                    Assert.False(task2Success.IsFaulted);

                    // A final write guarantees that the error is observed by OutputProducer,
                    // but doesn't return a canceled/faulted task.
                    var task4Success = outputProducer.WriteDataAsync(fullBuffer, cancellationToken: default(CancellationToken));
                    Assert.True(task4Success.IsCompleted);
                    Assert.False(task4Success.IsCanceled);
                    Assert.False(task4Success.IsFaulted);

                    // Third task is now canceled
                    await Assert.ThrowsAsync<OperationCanceledException>(() => task3Canceled);
                    Assert.True(task3Canceled.IsCanceled);

                    Assert.True(abortedSource.IsCancellationRequested);

                    await _mockLibuv.OnPostTask;

                    // Complete the 4th write
                    while (completeQueue.TryDequeue(out var triggerNextCompleted))
                    {
                        await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task CancelsBeforeWriteRequestCompletes(int maxResponseBufferSize)
        {
            await Task.Run(async () =>
            {
                var completeQueue = new ConcurrentQueue<Action<int>>();

                // Arrange
                _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                };

                var abortedSource = new CancellationTokenSource();

                var pipeOptions = new PipeOptions
                (
                    pool: _memoryPool,
                    readerScheduler: _libuvThread,
                    writerScheduler: PipeScheduler.Inline,
                    pauseWriterThreshold: maxResponseBufferSize,
                    resumeWriterThreshold: maxResponseBufferSize,
                    useSynchronizationContext: false
                );

                await using (var processor = CreateOutputProducer(pipeOptions))
                {
                    var outputProducer = processor.OutputProducer;
                    var bufferSize = maxResponseBufferSize - 1;

                    var data = new byte[bufferSize];
                    var fullBuffer = new ArraySegment<byte>(data, 0, bufferSize);

                    // Act
                    var task1Success = outputProducer.WriteDataAsync(fullBuffer, cancellationToken: abortedSource.Token);
                    // task1 should complete successfully as < _maxBytesPreCompleted

                    // First task is completed and successful
                    Assert.True(task1Success.IsCompleted);
                    Assert.False(task1Success.IsCanceled);
                    Assert.False(task1Success.IsFaulted);

                    // following tasks should wait.
                    var task3Canceled = outputProducer.WriteDataAsync(fullBuffer, cancellationToken: abortedSource.Token);

                    // Give time for tasks to percolate
                    await _mockLibuv.OnPostTask;

                    // Third task is not completed
                    Assert.False(task3Canceled.IsCompleted);
                    Assert.False(task3Canceled.IsCanceled);
                    Assert.False(task3Canceled.IsFaulted);

                    abortedSource.Cancel();

                    // Complete writes
                    while (completeQueue.TryDequeue(out var triggerNextCompleted))
                    {
                        await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                    }

                    // A final write guarantees that the error is observed by OutputProducer,
                    // but doesn't return a canceled/faulted task.
                    var task4Success = outputProducer.WriteDataAsync(fullBuffer);
                    Assert.True(task4Success.IsCompleted);
                    Assert.False(task4Success.IsCanceled);
                    Assert.False(task4Success.IsFaulted);

                    // Third task is now canceled
                    await Assert.ThrowsAsync<OperationCanceledException>(() => task3Canceled);
                    Assert.True(task3Canceled.IsCanceled);

                    Assert.True(abortedSource.IsCancellationRequested);

                    await _mockLibuv.OnPostTask;

                    // Complete the 4th write
                    while (completeQueue.TryDequeue(out var triggerNextCompleted))
                    {
                        await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task WriteAsyncWithTokenAfterCallWithoutIsCancelled(int maxResponseBufferSize)
        {
            await Task.Run(async () =>
            {
                var completeQueue = new ConcurrentQueue<Action<int>>();

                // Arrange
                _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                };

                var abortedSource = new CancellationTokenSource();

                var pipeOptions = new PipeOptions
                (
                    pool: _memoryPool,
                    readerScheduler: _libuvThread,
                    writerScheduler: PipeScheduler.Inline,
                    pauseWriterThreshold: maxResponseBufferSize,
                    resumeWriterThreshold: maxResponseBufferSize,
                    useSynchronizationContext: false
                );

                await using (var processor = CreateOutputProducer(pipeOptions))
                {
                    var outputProducer = processor.OutputProducer;
                    var bufferSize = maxResponseBufferSize;

                    var data = new byte[bufferSize];
                    var fullBuffer = new ArraySegment<byte>(data, 0, bufferSize);

                    // Act
                    var task1Waits = outputProducer.WriteDataAsync(fullBuffer);

                    // First task is not completed
                    Assert.False(task1Waits.IsCompleted);
                    Assert.False(task1Waits.IsCanceled);
                    Assert.False(task1Waits.IsFaulted);

                    // following tasks should wait.
                    var task2Canceled = outputProducer.WriteDataAsync(fullBuffer, cancellationToken: abortedSource.Token);

                    // Give time for tasks to percolate
                    await _mockLibuv.OnPostTask;

                    // Second task is not completed
                    Assert.False(task2Canceled.IsCompleted);
                    Assert.False(task2Canceled.IsCanceled);
                    Assert.False(task2Canceled.IsFaulted);

                    abortedSource.Cancel();

                    // Complete writes
                    while (completeQueue.TryDequeue(out var triggerNextCompleted))
                    {
                        await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                    }

                    await task1Waits.DefaultTimeout();

                    // First task is completed
                    Assert.True(task1Waits.IsCompleted);
                    Assert.False(task1Waits.IsCanceled);
                    Assert.False(task1Waits.IsFaulted);

                    // Second task is now canceled
                    await Assert.ThrowsAsync<OperationCanceledException>(() => task2Canceled);
                    Assert.True(task2Canceled.IsCanceled);

                    // A final write can still succeed.
                    var task3Success = outputProducer.WriteDataAsync(fullBuffer);

                    await _mockLibuv.OnPostTask;

                    // Complete the 3rd write
                    while (completeQueue.TryDequeue(out var triggerNextCompleted))
                    {
                        await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                    }

                    await task3Success.DefaultTimeout();

                    Assert.True(task3Success.IsCompleted);
                    Assert.False(task3Success.IsCanceled);
                    Assert.False(task3Success.IsFaulted);
                }
            });
        }

        [Theory]
        [MemberData(nameof(PositiveMaxResponseBufferSizeData))]
        public async Task WritesDontGetCompletedTooQuickly(int maxResponseBufferSize)
        {
            var completeQueue = new ConcurrentQueue<Action<int>>();

            // Arrange
            _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
            {
                completeQueue.Enqueue(triggerCompleted);
                return 0;
            };

            var pipeOptions = new PipeOptions
            (
                pool: _memoryPool,
                readerScheduler: _libuvThread,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: maxResponseBufferSize,
                resumeWriterThreshold: maxResponseBufferSize,
                useSynchronizationContext: false
            );

            await using (var processor = CreateOutputProducer(pipeOptions))
            {
                var outputProducer = processor.OutputProducer;
                var bufferSize = maxResponseBufferSize - 1;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

                // Act (Pre-complete the maximum number of bytes in preparation for the rest of the test)
                var writeTask1 = outputProducer.WriteDataAsync(buffer);

                // Assert
                // The first write should pre-complete since it is < _maxBytesPreCompleted.
                await _mockLibuv.OnPostTask;
                Assert.Equal(TaskStatus.RanToCompletion, writeTask1.Status);
                Assert.NotEmpty(completeQueue);

                // Act
                var writeTask2 = outputProducer.WriteDataAsync(buffer);
                var writeTask3 = outputProducer.WriteDataAsync(buffer);

                await _mockLibuv.OnPostTask;

                // Drain the write queue
                while (completeQueue.TryDequeue(out var triggerNextCompleted))
                {
                    await _libuvThread.PostAsync(cb => cb(0), triggerNextCompleted);
                }

                var timeout = TestConstants.DefaultTimeout;

                // Assert
                // Too many bytes are already pre-completed for the third but not the second write to pre-complete.
                // https://github.com/aspnet/KestrelHttpServer/issues/356
                await writeTask2.TimeoutAfter(timeout);
                await writeTask3.TimeoutAfter(timeout);
            }
        }

        [Theory]
        [MemberData(nameof(MaxResponseBufferSizeData))]
        public async Task WritesAreAggregated(long? maxResponseBufferSize)
        {
            var writeCalled = false;
            var writeCount = 0;

            _mockLibuv.OnWrite = (socket, buffers, triggerCompleted) =>
            {
                writeCount++;
                triggerCompleted(0);
                writeCalled = true;
                return 0;
            };

            // ConnectionHandler will set Pause/ResumeWriterThreshold to zero when MaxResponseBufferSize is null.
            // This is verified in PipeOptionsTests.OutputPipeOptionsConfiguredCorrectly.
            var pipeOptions = new PipeOptions
            (
                pool: _memoryPool,
                readerScheduler: _libuvThread,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: maxResponseBufferSize ?? 0,
                resumeWriterThreshold: maxResponseBufferSize ?? 0,
                useSynchronizationContext: false
            );

            await using (var processor = CreateOutputProducer(pipeOptions))
            {
                var outputProducer = processor.OutputProducer;
                _mockLibuv.KestrelThreadBlocker.Reset();

                var buffer = new ArraySegment<byte>(new byte[1]);

                // Two calls to WriteAsync trigger uv_write once if both calls
                // are made before write is scheduled
                var ignore = outputProducer.WriteDataAsync(buffer);
                ignore = outputProducer.WriteDataAsync(buffer);

                _mockLibuv.KestrelThreadBlocker.Set();

                await _mockLibuv.OnPostTask;

                Assert.True(writeCalled);
                writeCalled = false;

                // Write isn't called twice after the thread is unblocked
                await _mockLibuv.OnPostTask;

                Assert.False(writeCalled);
                // One call to ScheduleWrite
                Assert.Equal(1, _mockLibuv.PostCount);
                // One call to uv_write
                Assert.Equal(1, writeCount);
            }
        }

        private LibuvOuputProcessor CreateOutputProducer(PipeOptions pipeOptions, CancellationTokenSource cts = null)
        {
            var pair = DuplexPipe.CreateConnectionPair(pipeOptions, pipeOptions);

            var logger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext
            {
                Log = new TestKestrelTrace(logger),
                Scheduler = PipeScheduler.Inline
            };
            var transportContext = new TestLibuvTransportContext { Log = new LibuvTrace(logger) };

            var socket = new MockSocket(_mockLibuv, _libuvThread.Loop.ThreadId, transportContext.Log);
            var consumer = new LibuvOutputConsumer(pair.Application.Input, _libuvThread, socket, "0", transportContext.Log);

            var connectionFeatures = new FeatureCollection();
            connectionFeatures.Set(Mock.Of<IConnectionLifetimeFeature>());

            var http1Connection = new Http1Connection(new HttpConnectionContext
            {
                ServiceContext = serviceContext,
                ConnectionContext = Mock.Of<ConnectionContext>(),
                ConnectionFeatures = connectionFeatures,
                MemoryPool = _memoryPool,
                TimeoutControl = Mock.Of<ITimeoutControl>(),
                Transport = pair.Transport
            });

            if (cts != null)
            {
                http1Connection.RequestAborted.Register(cts.Cancel);
            }

            var outputTask = WriteOutputAsync(consumer, pair.Application.Input, http1Connection);

            var processor = new LibuvOuputProcessor
            {
                ProcessingTask = outputTask,
                OutputProducer = (Http1OutputProducer)http1Connection.Output,
                PipeWriter = pair.Transport.Output,
            };

            return processor;
        }

        private class LibuvOuputProcessor
        {
            public Http1OutputProducer OutputProducer { get; set; }
            public PipeWriter PipeWriter { get; set; }
            public Task ProcessingTask { get; set; }

            public async ValueTask DisposeAsync()
            {
                OutputProducer.Dispose();
                PipeWriter.Complete();

                await ProcessingTask;
            }
        }

        private async Task WriteOutputAsync(LibuvOutputConsumer consumer, PipeReader outputReader, Http1Connection http1Connection)
        {
            // This WriteOutputAsync() calling code is equivalent to that in LibuvConnection.
            try
            {
                // Ensure that outputReader.Complete() runs on the LibuvThread.
                // Without ConfigureAwait(false), xunit will dispatch.
                await consumer.WriteOutputAsync().ConfigureAwait(false);

                http1Connection.Abort(abortReason: null);
                outputReader.Complete();
            }
            catch (UvException ex)
            {
                http1Connection.Abort(new ConnectionAbortedException(ex.Message, ex));
                outputReader.Complete(ex);
            }
        }

        // Work around the internal type conflict (multiple assemblies have internalized this type and that fails with IVT)
        private class DuplexPipe : IDuplexPipe
        {
            public DuplexPipe(PipeReader reader, PipeWriter writer)
            {
                Input = reader;
                Output = writer;
            }

            public PipeReader Input { get; }

            public PipeWriter Output { get; }

            public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
            {
                var input = new Pipe(inputOptions);
                var output = new Pipe(outputOptions);

                var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
                var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

                return new DuplexPipePair(applicationToTransport, transportToApplication);
            }

            // This class exists to work around issues with value tuple on .NET Framework
            public readonly struct DuplexPipePair
            {
                public IDuplexPipe Transport { get; }
                public IDuplexPipe Application { get; }

                public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
                {
                    Transport = transport;
                    Application = application;
                }
            }
        }
    }
}
