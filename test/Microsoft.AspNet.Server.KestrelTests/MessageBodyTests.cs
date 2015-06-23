// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for MessageBodyTests
    /// </summary>
    public class MessageBodyTests
    {
        [Fact]
        public async Task Http10ConnectionClose()
        {
            var input = new TestInput();
            var body = MessageBody.For("HTTP/1.0", new Dictionary<string, string[]>(), input.FrameContext);
            var stream = new FrameRequestStream(body);

            input.Add("Hello", true);
            body.Consume();

            var buffer1 = new byte[1024];
            var count1 = stream.Read(buffer1, 0, 1024);
            AssertASCII("Hello", new ArraySegment<byte>(buffer1, 0, 5));

            var buffer2 = new byte[1024];
            var count2 = stream.Read(buffer2, 0, 1024);
            Assert.Equal(0, count2);
        }

        [Fact]
        public async Task Http10ConnectionCloseAsync()
        {
            var input = new TestInput();
            var body = MessageBody.For("HTTP/1.0", new Dictionary<string, string[]>(), input.FrameContext);
            var stream = new FrameRequestStream(body);

            input.Add("Hello", true);
            body.Consume();

            var buffer1 = new byte[1024];
            var count1 = await stream.ReadAsync(buffer1, 0, 1024);
            AssertASCII("Hello", new ArraySegment<byte>(buffer1, 0, 5));

            var buffer2 = new byte[1024];
            var count2 = await stream.ReadAsync(buffer2, 0, 1024);
            Assert.Equal(0, count2);
        }

        private void AssertASCII(string expected, ArraySegment<byte> actual)
        {
            var encoding = Encoding.ASCII;
            var bytes = encoding.GetBytes(expected);
            Assert.Equal(bytes.Length, actual.Count);
            for (var index = 0; index != bytes.Length; ++index)
            {
                Assert.Equal(bytes[index], actual.Array[actual.Offset + index]);
            }
        }
    }
}