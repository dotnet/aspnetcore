// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class SocketInputTests
    {
        public static readonly TheoryData<Mock<IBufferSizeControl>> MockBufferSizeControlData =
            new TheoryData<Mock<IBufferSizeControl>>() { new Mock<IBufferSizeControl>(), null };

        [Theory]
        [MemberData(nameof(MockBufferSizeControlData))]
        public void IncomingDataCallsBufferSizeControlAdd(Mock<IBufferSizeControl> mockBufferSizeControl)
        {
            using (var memory = new MemoryPool())
            using (var socketInput = new SocketInput(memory, null, mockBufferSizeControl?.Object))
            {
                socketInput.IncomingData(new byte[5], 0, 5);
                mockBufferSizeControl?.Verify(b => b.Add(5));
            }
        }

        [Theory]
        [MemberData(nameof(MockBufferSizeControlData))]
        public void IncomingCompleteCallsBufferSizeControlAdd(Mock<IBufferSizeControl> mockBufferSizeControl)
        {
            using (var memory = new MemoryPool())
            using (var socketInput = new SocketInput(memory, null, mockBufferSizeControl?.Object))
            {
                socketInput.IncomingComplete(5, null);
                mockBufferSizeControl?.Verify(b => b.Add(5));
            }
        }

        [Theory]
        [MemberData(nameof(MockBufferSizeControlData))]
        public void ConsumingCompleteCallsBufferSizeControlSubtract(Mock<IBufferSizeControl> mockBufferSizeControl)
        {
            using (var kestrelEngine = new KestrelEngine(new MockLibuv(), new TestServiceContext()))
            {
                kestrelEngine.Start(1);

                using (var memory = new MemoryPool())
                using (var socketInput = new SocketInput(memory, null, mockBufferSizeControl?.Object))
                {
                    socketInput.IncomingData(new byte[20], 0, 20);

                    var iterator = socketInput.ConsumingStart();
                    iterator.Skip(5);
                    socketInput.ConsumingComplete(iterator, iterator);
                    mockBufferSizeControl?.Verify(b => b.Subtract(5));
                }
            }
        }

        [Fact]
        public async Task ConcurrentReadsFailGracefully()
        {
            // Arrange
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            using (var memory2 = new MemoryPool())
            using (var socketInput = new SocketInput(memory2, ltp))
            {
                var task0Threw = false;
                var task1Threw = false;
                var task2Threw = false;

                var task0 = AwaitAsTaskAsync(socketInput);

                Assert.False(task0.IsFaulted);

                var task = task0.ContinueWith(
                    (t) =>
                    {
                        TestConcurrentFaultedTask(t);
                        task0Threw = true;
                    },
                    TaskContinuationOptions.OnlyOnFaulted);

                Assert.False(task0.IsFaulted);

                // Awaiting/continuing two tasks faults both

                var task1 = AwaitAsTaskAsync(socketInput);

                await task1.ContinueWith(
                    (t) =>
                    {
                        TestConcurrentFaultedTask(t);
                        task1Threw = true;
                    },
                    TaskContinuationOptions.OnlyOnFaulted);

                await task;

                Assert.True(task0.IsFaulted);
                Assert.True(task1.IsFaulted);

                Assert.True(task0Threw);
                Assert.True(task1Threw);

                // socket stays faulted

                var task2 = AwaitAsTaskAsync(socketInput);

                await task2.ContinueWith(
                    (t) =>
                    {
                        TestConcurrentFaultedTask(t);
                        task2Threw = true;
                    },
                    TaskContinuationOptions.OnlyOnFaulted);

                Assert.True(task2.IsFaulted);
                Assert.True(task2Threw);
            }
        }

        [Fact]
        public void ConsumingOutOfOrderFailsGracefully()
        {
            var defultIter = new MemoryPoolIterator();

            // Calling ConsumingComplete without a preceding calling to ConsumingStart fails
            using (var socketInput = new SocketInput(null, null))
            {
                Assert.Throws<InvalidOperationException>(() => socketInput.ConsumingComplete(defultIter, defultIter));
            }

            // Calling ConsumingComplete twice in a row fails
            using (var socketInput = new SocketInput(null, null))
            {
                socketInput.ConsumingStart();
                socketInput.ConsumingComplete(defultIter, defultIter);
                Assert.Throws<InvalidOperationException>(() => socketInput.ConsumingComplete(defultIter, defultIter));
            }

            // Calling ConsumingStart twice in a row fails
            using (var socketInput = new SocketInput(null, null))
            {
                socketInput.ConsumingStart();
                Assert.Throws<InvalidOperationException>(() => socketInput.ConsumingStart());
            }
        }

        [Fact]
        public async Task PeekAsyncRereturnsTheSameData()
        {
            using (var memory = new MemoryPool())
            using (var socketInput = new SocketInput(memory, new SynchronousThreadPool()))
            {
                socketInput.IncomingData(new byte[5], 0, 5);

                Assert.True(socketInput.IsCompleted);
                Assert.Equal(5, (await socketInput.PeekAsync()).Count);

                // The same 5 bytes will be returned again since it hasn't been consumed.
                Assert.True(socketInput.IsCompleted);
                Assert.Equal(5, (await socketInput.PeekAsync()).Count);

                var scan = socketInput.ConsumingStart();
                scan.Skip(3);
                socketInput.ConsumingComplete(scan, scan);

                // The remaining 2 unconsumed bytes will be returned.
                Assert.True(socketInput.IsCompleted);
                Assert.Equal(2, (await socketInput.PeekAsync()).Count);

                scan = socketInput.ConsumingStart();
                scan.Skip(2);
                socketInput.ConsumingComplete(scan, scan);

                // Everything has been consume so socketInput is no longer in the completed state
                Assert.False(socketInput.IsCompleted);
            }
        }

        [Fact]
        public async Task CompleteAwaitingDoesNotCauseZeroLengthRead()
        {
            using (var memory = new MemoryPool())
            using (var socketInput = new SocketInput(memory, new SynchronousThreadPool()))
            {
                var readBuffer = new byte[20];

                socketInput.IncomingData(new byte[5], 0, 5);
                Assert.Equal(5, await socketInput.ReadAsync(readBuffer, 0, 20));

                var readTask = socketInput.ReadAsync(readBuffer, 0, 20);
                socketInput.CompleteAwaiting();
                Assert.False(readTask.IsCompleted);

                socketInput.IncomingData(new byte[5], 0, 5);
                Assert.Equal(5, await readTask);
            }
        }

        [Fact]
        public async Task CompleteAwaitingDoesNotCauseZeroLengthPeek()
        {
            using (var memory = new MemoryPool())
            using (var socketInput = new SocketInput(memory, new SynchronousThreadPool()))
            {
                socketInput.IncomingData(new byte[5], 0, 5);
                Assert.Equal(5, (await socketInput.PeekAsync()).Count);

                var scan = socketInput.ConsumingStart();
                scan.Skip(5);
                socketInput.ConsumingComplete(scan, scan);

                var peekTask = socketInput.PeekAsync();
                socketInput.CompleteAwaiting();
                Assert.False(peekTask.IsCompleted);

                socketInput.IncomingData(new byte[5], 0, 5);
                Assert.Equal(5, (await socketInput.PeekAsync()).Count);
            }
        }

        private static void TestConcurrentFaultedTask(Task t)
        {
            Assert.True(t.IsFaulted);
            Assert.IsType(typeof(System.InvalidOperationException), t.Exception.InnerException);
            Assert.Equal(t.Exception.InnerException.Message, "Concurrent reads are not supported.");
        }

        private async Task AwaitAsTaskAsync(SocketInput socketInput)
        {
            await socketInput;
        }
    }
}
