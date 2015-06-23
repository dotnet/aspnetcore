// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Http;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for MessageBodyExchangerTests
    /// </summary>
    public class MessageBodyExchangerTests
    {
	    [Fact]
        public async Task CallingReadAsyncBeforeTransfer()
        {
            var testInput = new TestInput();
            var context = new ConnectionContext();
            context.SocketInput = new SocketInput(new MemoryPool());

            var exchanger = new MessageBodyExchanger(testInput.FrameContext);

            var buffer1 = new byte[1024];
            var buffer2 = new byte[1024];
            var task1 = exchanger.ReadAsync(new ArraySegment<byte>(buffer1));
            var task2 = exchanger.ReadAsync(new ArraySegment<byte>(buffer2));

            Assert.False(task1.IsCompleted);
            Assert.False(task2.IsCompleted);

            testInput.Add("Hello");

            exchanger.Transfer(3, false);

            var count1 = await task1;

            Assert.True(task1.IsCompleted);
            Assert.False(task2.IsCompleted);
            AssertASCII("Hel", new ArraySegment<byte>(buffer1, 0, count1));

            exchanger.Transfer(2, false);

            var count2 = await task2;

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            AssertASCII("lo", new ArraySegment<byte>(buffer2, 0, count2));
        }

        [Fact]
        public async Task CallingTransferBeforeReadAsync()
        {
            var testInput = new TestInput();
            var context = new ConnectionContext();
            context.SocketInput = new SocketInput(new MemoryPool());

            var exchanger = new MessageBodyExchanger(testInput.FrameContext);

            testInput.Add("Hello");

            exchanger.Transfer(5, false);

            var buffer1 = new byte[1024];
            var buffer2 = new byte[1024];
            var task1 = exchanger.ReadAsync(new ArraySegment<byte>(buffer1));
            var task2 = exchanger.ReadAsync(new ArraySegment<byte>(buffer2));

            Assert.True(task1.IsCompleted);
            Assert.False(task2.IsCompleted);

            await task1;

            var count1 = await task1;

            Assert.True(task1.IsCompleted);
            Assert.False(task2.IsCompleted);
            AssertASCII("Hello", new ArraySegment<byte>(buffer1, 0, count1));
        }

        [Fact]
        public async Task TransferZeroBytesDoesNotReleaseReadAsync()
        {
            var testInput = new TestInput();
            var context = new ConnectionContext();
            context.SocketInput = new SocketInput(new MemoryPool());

            var exchanger = new MessageBodyExchanger(testInput.FrameContext);

            var buffer1 = new byte[1024];
            var buffer2 = new byte[1024];
            var task1 = exchanger.ReadAsync(new ArraySegment<byte>(buffer1));
            var task2 = exchanger.ReadAsync(new ArraySegment<byte>(buffer2));

            Assert.False(task1.IsCompleted);
            Assert.False(task2.IsCompleted);

            testInput.Add("Hello");

            exchanger.Transfer(3, false);

            var count1 = await task1;

            Assert.True(task1.IsCompleted);
            Assert.False(task2.IsCompleted);
            AssertASCII("Hel", new ArraySegment<byte>(buffer1, 0, count1));

            exchanger.Transfer(0, false);

            Assert.True(task1.IsCompleted);
            Assert.False(task2.IsCompleted);
        }

        [Fact]
        public async Task TransferFinDoesReleaseReadAsync()
        {
            var testInput = new TestInput();
            var context = new ConnectionContext();
            context.SocketInput = new SocketInput(new MemoryPool());

            var exchanger = new MessageBodyExchanger(testInput.FrameContext);

            var buffer1 = new byte[1024];
            var buffer2 = new byte[1024];
            var task1 = exchanger.ReadAsync(new ArraySegment<byte>(buffer1));
            var task2 = exchanger.ReadAsync(new ArraySegment<byte>(buffer2));

            Assert.False(task1.IsCompleted);
            Assert.False(task2.IsCompleted);

            testInput.Add("Hello");

            exchanger.Transfer(3, false);

            var count1 = await task1;

            Assert.True(task1.IsCompleted);
            Assert.False(task2.IsCompleted);
            AssertASCII("Hel", new ArraySegment<byte>(buffer1, 0, count1));

            exchanger.Transfer(0, true);

            var count2 = await task2;

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(0, count2);
        }


        [Fact]
        public async Task TransferFinFirstDoesReturnsCompletedReadAsyncs()
        {

            var testInput = new TestInput();
            var context = new ConnectionContext();
            context.SocketInput = new SocketInput(new MemoryPool());

            var exchanger = new MessageBodyExchanger(testInput.FrameContext);

            testInput.Add("Hello");

            exchanger.Transfer(5, true);

            var buffer1 = new byte[1024];
            var buffer2 = new byte[1024];
            var task1 = exchanger.ReadAsync(new ArraySegment<byte>(buffer1));
            var task2 = exchanger.ReadAsync(new ArraySegment<byte>(buffer2));

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);

            var count1 = await task1;
            var count2 = await task2;

            AssertASCII("Hello", new ArraySegment<byte>(buffer1, 0, count1));
            Assert.Equal(0, count2);
        }

        private void AssertASCII(string expected, ArraySegment<byte> actual)
        {
            var encoding = System.Text.Encoding.ASCII;
            var bytes = encoding.GetBytes(expected);
            Assert.Equal(bytes.Length, actual.Count);
            for (var index = 0; index != bytes.Length; ++index)
            {
                Assert.Equal(bytes[index], actual.Array[actual.Offset + index]);
            }
        }
    }
}