// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Sockets.Tests;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Formatters.Tests
{
    public class ServerSentEventsMessageFormatterTests
    {
        [Fact]
        public void InsufficientWriteBufferSpace()
        {
            const int ExpectedSize = 23;
            var message = MessageTestUtils.CreateMessage("Test", MessageType.Text);

            byte[] buffer;
            int bufferSize;
            int written;
            for (bufferSize = 0; bufferSize < 23; bufferSize++)
            {
                buffer = new byte[bufferSize];
                Assert.False(ServerSentEventsMessageFormatter.TryFormatMessage(message, buffer, out written));
                Assert.Equal(0, written);
            }

            buffer = new byte[bufferSize];
            Assert.True(ServerSentEventsMessageFormatter.TryFormatMessage(message, buffer, out written));
            Assert.Equal(ExpectedSize, written);
        }

        [Fact]
        public void WriteInvalidMessages()
        {
            var message = new Message(new byte[0], MessageType.Binary, endOfMessage: false);
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ServerSentEventsMessageFormatter.TryFormatMessage(message, Span<byte>.Empty, out var written));
            Assert.Equal("Cannot format message where endOfMessage is false using this format", ex.Message);
        }

        [Theory]
        [InlineData("data: T\r\n\r\n", MessageType.Text, "")]
        [InlineData("data: T\r\ndata: Hello, World\r\n\r\n", MessageType.Text, "Hello, World")]
        [InlineData("data: T\r\ndata: Hello\r\ndata: World\r\n\r\n", MessageType.Text, "Hello\r\nWorld")]
        [InlineData("data: T\r\ndata: Hello\r\ndata: World\r\n\r\n", MessageType.Text, "Hello\nWorld")]
        [InlineData("data: T\r\ndata: Hello\r\ndata: \r\n\r\n", MessageType.Text, "Hello\n")]
        [InlineData("data: T\r\ndata: Hello\r\ndata: \r\n\r\n", MessageType.Text, "Hello\r\n")]
        [InlineData("data: C\r\n\r\n", MessageType.Close, "")]
        [InlineData("data: C\r\ndata: Hello, World\r\n\r\n", MessageType.Close, "Hello, World")]
        [InlineData("data: C\r\ndata: Hello\r\ndata: World\r\n\r\n", MessageType.Close, "Hello\r\nWorld")]
        [InlineData("data: C\r\ndata: Hello\r\ndata: World\r\n\r\n", MessageType.Close, "Hello\nWorld")]
        [InlineData("data: C\r\ndata: Hello\r\ndata: \r\n\r\n", MessageType.Close, "Hello\n")]
        [InlineData("data: C\r\ndata: Hello\r\ndata: \r\n\r\n", MessageType.Close, "Hello\r\n")]
        [InlineData("data: E\r\n\r\n", MessageType.Error, "")]
        [InlineData("data: E\r\ndata: Hello, World\r\n\r\n", MessageType.Error, "Hello, World")]
        [InlineData("data: E\r\ndata: Hello\r\ndata: World\r\n\r\n", MessageType.Error, "Hello\r\nWorld")]
        [InlineData("data: E\r\ndata: Hello\r\ndata: World\r\n\r\n", MessageType.Error, "Hello\nWorld")]
        [InlineData("data: E\r\ndata: Hello\r\ndata: \r\n\r\n", MessageType.Error, "Hello\n")]
        [InlineData("data: E\r\ndata: Hello\r\ndata: \r\n\r\n", MessageType.Error, "Hello\r\n")]
        public void WriteTextMessage(string encoded, MessageType messageType, string payload)
        {
            var message = MessageTestUtils.CreateMessage(payload, messageType);

            var buffer = new byte[256];
            Assert.True(ServerSentEventsMessageFormatter.TryFormatMessage(message, buffer, out var written));

            Assert.Equal(encoded, Encoding.UTF8.GetString(buffer, 0, written));
        }

        [Theory]
        [InlineData("data: B\r\n\r\n", new byte[0])]
        [InlineData("data: B\r\ndata: q83v\r\n\r\n", new byte[] { 0xAB, 0xCD, 0xEF })]
        public void WriteBinaryMessage(string encoded, byte[] payload)
        {
            var message = MessageTestUtils.CreateMessage(payload);

            var buffer = new byte[256];
            Assert.True(ServerSentEventsMessageFormatter.TryFormatMessage(message, buffer, out var written));

            Assert.Equal(encoded, Encoding.UTF8.GetString(buffer, 0, written));
        }
    }
}
