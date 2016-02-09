// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    /// <summary>
    /// Summary description for MessageBodyTests
    /// </summary>
    public class MessageBodyTests
    {
        [Fact]
        public void Http10ConnectionClose()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For("HTTP/1.0", new FrameRequestHeaders(), input.FrameContext);
                var stream = new FrameRequestStream().StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                var count1 = stream.Read(buffer1, 0, 1024);
                AssertASCII("Hello", new ArraySegment<byte>(buffer1, 0, 5));

                var buffer2 = new byte[1024];
                var count2 = stream.Read(buffer2, 0, 1024);
                Assert.Equal(0, count2);
            }
        }

        [Fact]
        public async Task Http10ConnectionCloseAsync()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For("HTTP/1.0", new FrameRequestHeaders(), input.FrameContext);
                var stream = new FrameRequestStream().StartAcceptingReads(body);

                input.Add("Hello", true);

                var buffer1 = new byte[1024];
                var count1 = await stream.ReadAsync(buffer1, 0, 1024);
                AssertASCII("Hello", new ArraySegment<byte>(buffer1, 0, 5));

                var buffer2 = new byte[1024];
                var count2 = await stream.ReadAsync(buffer2, 0, 1024);
                Assert.Equal(0, count2);
            }
        }

        [Fact]
        public async Task CanHandleLargeBlocks()
        {
            using (var input = new TestInput())
            {
                var body = MessageBody.For("HTTP/1.0", new FrameRequestHeaders(), input.FrameContext);
                var stream = new FrameRequestStream().StartAcceptingReads(body);

                // Input needs to be greater than 4032 bytes to allocate a block not backed by a slab.
                var largeInput = new string('a', 8192);

                input.Add(largeInput, true);
                // Add a smaller block to the end so that SocketInput attempts to return the large
                // block to the memory pool.
                input.Add("Hello", true);

                var readBuffer = new byte[8192];

                var count1 = await stream.ReadAsync(readBuffer, 0, 8192);
                Assert.Equal(8192, count1);
                AssertASCII(largeInput, new ArraySegment<byte>(readBuffer, 0, 8192));

                var count2 = await stream.ReadAsync(readBuffer, 0, 8192);
                Assert.Equal(5, count2);
                AssertASCII("Hello", new ArraySegment<byte>(readBuffer, 0, 5));

                var count3 = await stream.ReadAsync(readBuffer, 0, 8192);
                Assert.Equal(0, count3);
            }
        }

        private void AssertASCII(string expected, ArraySegment<byte> actual)
        {
            var encoding = Encoding.ASCII;
            var bytes = encoding.GetBytes(expected);
            Assert.Equal(bytes.Length, actual.Count);
            for (var index = 0; index < bytes.Length; index++)
            {
                Assert.Equal(bytes[index], actual.Array[actual.Offset + index]);
            }
        }
    }
}