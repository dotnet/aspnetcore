// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    public partial class WebSocketConnectionTests
    {
        [Theory]
        [InlineData(new byte[] { 0x81, 0x00 }, "", true)]
        [InlineData(new byte[] { 0x81, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F }, "Hello", true)]
        [InlineData(new byte[] { 0x81, 0x85, 0x1, 0x2, 0x3, 0x4, 0x48 ^ 0x1, 0x65 ^ 0x2, 0x6C ^ 0x3, 0x6C ^ 0x4, 0x6F ^ 0x1 }, "Hello", true)]
        [InlineData(new byte[] { 0x01, 0x00 }, "", false)]
        [InlineData(new byte[] { 0x01, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F }, "Hello", false)]
        [InlineData(new byte[] { 0x01, 0x85, 0x1, 0x2, 0x3, 0x4, 0x48 ^ 0x1, 0x65 ^ 0x2, 0x6C ^ 0x3, 0x6C ^ 0x4, 0x6F ^ 0x1 }, "Hello", false)]
        public Task ReadTextFrames(byte[] rawFrame, string message, bool endOfMessage)
        {
            return RunSingleFrameTest(
                rawFrame,
                endOfMessage,
                WebSocketOpcode.Text,
                b => Assert.Equal(message, Encoding.UTF8.GetString(b)));
        }

        [Theory]
        // Opcode = Binary
        [InlineData(new byte[] { 0x82, 0x00 }, new byte[0], WebSocketOpcode.Binary, true)]
        [InlineData(new byte[] { 0x82, 0x05, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Binary, true)]
        [InlineData(new byte[] { 0x82, 0x85, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Binary, true)]
        [InlineData(new byte[] { 0x02, 0x00 }, new byte[0], WebSocketOpcode.Binary, false)]
        [InlineData(new byte[] { 0x02, 0x05, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Binary, false)]
        [InlineData(new byte[] { 0x02, 0x85, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Binary, false)]

        // Opcode = Ping
        [InlineData(new byte[] { 0x89, 0x00 }, new byte[0], WebSocketOpcode.Ping, true)]
        [InlineData(new byte[] { 0x89, 0x05, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Ping, true)]
        [InlineData(new byte[] { 0x89, 0x85, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Ping, true)]
        // Control frames can't have fin=false

        // Opcode = Pong
        [InlineData(new byte[] { 0x8A, 0x00 }, new byte[0], WebSocketOpcode.Pong, true)]
        [InlineData(new byte[] { 0x8A, 0x05, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Pong, true)]
        [InlineData(new byte[] { 0x8A, 0x85, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Pong, true)]
        // Control frames can't have fin=false
        public Task ReadBinaryFormattedFrames(byte[] rawFrame, byte[] payload, WebSocketOpcode opcode, bool endOfMessage)
        {
            return RunSingleFrameTest(
                rawFrame,
                endOfMessage,
                opcode,
                b => Assert.Equal(payload, b));
        }

        [Fact]
        public async Task ReadMultipleFramesAcrossMultipleBuffers()
        {
            var result = await RunReceiveTest(
                producer: async (channel, cancellationToken) =>
                {
                    await channel.WriteAsync(new byte[] { 0x02, 0x05 }.Slice()).OrTimeout();
                    await Task.Yield();
                    await channel.WriteAsync(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB, 0x80, 0x05 }.Slice()).OrTimeout();
                    await Task.Yield();
                    await channel.WriteAsync(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }.Slice()).OrTimeout();
                    await Task.Yield();
                    await channel.WriteAsync(new byte[] { 0xAB }.Slice()).OrTimeout();
                    await Task.Yield();
                });

            Assert.Equal(2, result.Received.Count);

            Assert.False(result.Received[0].EndOfMessage);
            Assert.Equal(WebSocketOpcode.Binary, result.Received[0].Opcode);
            Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, result.Received[0].Payload.ToArray());

            Assert.True(result.Received[1].EndOfMessage);
            Assert.Equal(WebSocketOpcode.Continuation, result.Received[1].Opcode);
            Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, result.Received[1].Payload.ToArray());
        }

        [Fact]
        public async Task ReadLargeMaskedPayload()
        {
            // This test was added to ensure we don't break a behavior discovered while running the Autobahn test suite.

            // Larger than one page, which means it will span blocks in the memory pool.
            var expectedPayload = new byte[4192];
            for (int i = 0; i < expectedPayload.Length; i++)
            {
                expectedPayload[i] = (byte)(i % byte.MaxValue);
            }
            var maskingKey = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var sendPayload = new byte[4192];
            for (int i = 0; i < expectedPayload.Length; i++)
            {
                sendPayload[i] = (byte)(expectedPayload[i] ^ maskingKey[i % 4]);
            }

            var result = await RunReceiveTest(
                producer: async (channel, cancellationToken) =>
                {
                    // We use a 64-bit length because we want to ensure that the first page of data ends at an
                    // offset within the frame that is NOT divisible by 4. This ensures that when the unmasking
                    // moves from one buffer to the other, we are at a non-zero position within the masking key.
                    // This ensures that we're tracking the masking key offset properly.

                    // Header: (Opcode=Binary, Fin=true), (Mask=false, Len=126), (64-bit big endian length)
                    await channel.WriteAsync(new byte[] { 0x82, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x60 }).OrTimeout();
                    await channel.WriteAsync(maskingKey).OrTimeout();
                    await Task.Yield();
                    await channel.WriteAsync(sendPayload).OrTimeout();
                });

            Assert.Equal(1, result.Received.Count);

            var frame = result.Received[0];
            Assert.True(frame.EndOfMessage);
            Assert.Equal(WebSocketOpcode.Binary, frame.Opcode);
            Assert.Equal(expectedPayload, frame.Payload.ToArray());
        }

        [Fact]
        public async Task Read16BitPayloadLength()
        {
            var expectedPayload = new byte[1024];
            new Random().NextBytes(expectedPayload);

            var result = await RunReceiveTest(
                producer: async (channel, cancellationToken) =>
                {
                    // Header: (Opcode=Binary, Fin=true), (Mask=false, Len=126), (16-bit big endian length)
                    await channel.WriteAsync(new byte[] { 0x82, 0x7E, 0x04, 0x00 }).OrTimeout();
                    await Task.Yield();
                    await channel.WriteAsync(expectedPayload).OrTimeout();
                });

            Assert.Equal(1, result.Received.Count);

            var frame = result.Received[0];
            Assert.True(frame.EndOfMessage);
            Assert.Equal(WebSocketOpcode.Binary, frame.Opcode);
            Assert.Equal(expectedPayload, frame.Payload.ToArray());
        }

        [Fact]
        public async Task Read64bitPayloadLength()
        {
            // Allocating an actual (2^32 + 1) byte payload is crazy for this test. We just need to test that we can USE a 64-bit length
            var expectedPayload = new byte[1024];
            new Random().NextBytes(expectedPayload);

            var result = await RunReceiveTest(
                producer: async (channel, cancellationToken) =>
                {
                    // Header: (Opcode=Binary, Fin=true), (Mask=false, Len=127), (64-bit big endian length)
                    await channel.WriteAsync(new byte[] { 0x82, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00 }).OrTimeout();
                    await Task.Yield();
                    await channel.WriteAsync(expectedPayload).OrTimeout();
                });

            Assert.Equal(1, result.Received.Count);

            var frame = result.Received[0];
            Assert.True(frame.EndOfMessage);
            Assert.Equal(WebSocketOpcode.Binary, frame.Opcode);
            Assert.Equal(expectedPayload, frame.Payload.ToArray());
        }

        private static async Task RunSingleFrameTest(byte[] rawFrame, bool endOfMessage, WebSocketOpcode expectedOpcode, Action<byte[]> payloadAssert)
        {
            var result = await RunReceiveTest(
                producer: async (channel, cancellationToken) =>
                {
                    await channel.WriteAsync(rawFrame.Slice()).OrTimeout();
                });
            var frames = result.Received;
            Assert.Equal(1, frames.Count);

            var frame = frames[0];

            Assert.Equal(endOfMessage, frame.EndOfMessage);
            Assert.Equal(expectedOpcode, frame.Opcode);
            payloadAssert(frame.Payload.ToArray());
        }

        private static async Task<WebSocketConnectionSummary> RunReceiveTest(Func<IPipeWriter, CancellationToken, Task> producer)
        {
            using (var factory = new PipeFactory())
            {
                var outbound = factory.Create();
                var inbound = factory.Create();

                var timeoutToken = TestUtil.CreateTimeoutToken();

                var producerTask = Task.Run(async () =>
                {
                    await producer(inbound.Writer, timeoutToken).OrTimeout();
                    inbound.Writer.Complete();
                }, timeoutToken);

                var consumerTask = Task.Run(async () =>
                {
                    var connection = new WebSocketConnection(inbound.Reader, outbound.Writer, options: new WebSocketOptions().WithAllFramesPassedThrough());
                    using (timeoutToken.Register(() => connection.Dispose()))
                    using (connection)
                    {
                        // Receive frames until we're closed
                        return await connection.ExecuteAndCaptureFramesAsync().OrTimeout();
                    }
                }, timeoutToken);

                await Task.WhenAll(producerTask, consumerTask);
                return consumerTask.Result;
            }
        }

        private static WebSocketFrame CreateTextFrame(string message)
        {
            var payload = Encoding.UTF8.GetBytes(message);
            return CreateFrame(endOfMessage: true, opcode: WebSocketOpcode.Text, payload: payload);
        }

        private static WebSocketFrame CreateBinaryFrame(byte[] payload)
        {
            return CreateFrame(endOfMessage: true, opcode: WebSocketOpcode.Binary, payload: payload);
        }

        private static WebSocketFrame CreateFrame(bool endOfMessage, WebSocketOpcode opcode, byte[] payload)
        {
            return new WebSocketFrame(endOfMessage, opcode, payload: ReadableBuffer.Create(payload, 0, payload.Length));
        }
    }
}
