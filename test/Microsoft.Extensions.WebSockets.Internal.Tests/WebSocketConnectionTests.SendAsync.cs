// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.Extensions.Internal;
using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    public partial class WebSocketConnectionTests
    {
        // No auto-pinging for us!
        private readonly static WebSocketOptions DefaultTestOptions = new WebSocketOptions().WithAllFramesPassedThrough();

        [Theory]
        [InlineData("", true, new byte[] { 0x81, 0x00 })]
        [InlineData("Hello", true, new byte[] { 0x81, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F })]
        [InlineData("", false, new byte[] { 0x01, 0x00 })]
        [InlineData("Hello", false, new byte[] { 0x01, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F })]
        public async Task WriteTextFrames(string message, bool endOfMessage, byte[] expectedRawFrame)
        {
            var data = await RunSendTest(
                producer: async (socket) =>
                {
                    var payload = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(CreateFrame(
                        endOfMessage,
                        opcode: WebSocketOpcode.Text,
                        payload: payload)).OrTimeout();
                }, options: DefaultTestOptions);
            Assert.Equal(expectedRawFrame, data);
        }

        [Theory]
        // Opcode = Binary
        [InlineData(new byte[0], WebSocketOpcode.Binary, true, new byte[] { 0x82, 0x00 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Binary, true, new byte[] { 0x82, 0x05, 0xA, 0xB, 0xC, 0xD, 0xE })]
        [InlineData(new byte[0], WebSocketOpcode.Binary, false, new byte[] { 0x02, 0x00 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Binary, false, new byte[] { 0x02, 0x05, 0xA, 0xB, 0xC, 0xD, 0xE })]

        // Opcode = Continuation
        [InlineData(new byte[0], WebSocketOpcode.Continuation, true, new byte[] { 0x80, 0x00 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Continuation, true, new byte[] { 0x80, 0x05, 0xA, 0xB, 0xC, 0xD, 0xE })]
        [InlineData(new byte[0], WebSocketOpcode.Continuation, false, new byte[] { 0x00, 0x00 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Continuation, false, new byte[] { 0x00, 0x05, 0xA, 0xB, 0xC, 0xD, 0xE })]

        // Opcode = Ping
        [InlineData(new byte[0], WebSocketOpcode.Ping, true, new byte[] { 0x89, 0x00 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Ping, true, new byte[] { 0x89, 0x05, 0xA, 0xB, 0xC, 0xD, 0xE })]
        [InlineData(new byte[0], WebSocketOpcode.Ping, false, new byte[] { 0x09, 0x00 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Ping, false, new byte[] { 0x09, 0x05, 0xA, 0xB, 0xC, 0xD, 0xE })]

        // Opcode = Pong
        [InlineData(new byte[0], WebSocketOpcode.Pong, true, new byte[] { 0x8A, 0x00 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Pong, true, new byte[] { 0x8A, 0x05, 0xA, 0xB, 0xC, 0xD, 0xE })]
        [InlineData(new byte[0], WebSocketOpcode.Pong, false, new byte[] { 0x0A, 0x00 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Pong, false, new byte[] { 0x0A, 0x05, 0xA, 0xB, 0xC, 0xD, 0xE })]
        public async Task WriteBinaryFormattedFrames(byte[] payload, WebSocketOpcode opcode, bool endOfMessage, byte[] expectedRawFrame)
        {
            var data = await RunSendTest(
                producer: async (socket) =>
                {
                    await socket.SendAsync(CreateFrame(
                        endOfMessage,
                        opcode,
                        payload: payload)).OrTimeout();
                }, options: DefaultTestOptions);
            Assert.Equal(expectedRawFrame, data);
        }

        [Theory]
        [InlineData("", new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x81, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData("Hello", new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x81, 0x85, 0x01, 0x02, 0x03, 0x04, 0x48 ^ 0x01, 0x65 ^ 0x02, 0x6C ^ 0x03, 0x6C ^ 0x04, 0x6F ^ 0x01 })]
        public async Task WriteMaskedTextFrames(string message, byte[] maskingKey, byte[] expectedRawFrame)
        {
            var data = await RunSendTest(
                producer: async (socket) =>
                {
                    var payload = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(CreateFrame(
                        endOfMessage: true,
                        opcode: WebSocketOpcode.Text,
                        payload: payload)).OrTimeout();
                }, options: DefaultTestOptions.WithFixedMaskingKey(maskingKey));
            Assert.Equal(expectedRawFrame, data);
        }

        [Theory]
        // Opcode = Binary
        [InlineData(new byte[0], WebSocketOpcode.Binary, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x82, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Binary, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x82, 0x85, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
        [InlineData(new byte[0], WebSocketOpcode.Binary, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x02, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Binary, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x02, 0x85, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]

        // Opcode = Continuation
        [InlineData(new byte[0], WebSocketOpcode.Continuation, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x80, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Continuation, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x80, 0x85, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
        [InlineData(new byte[0], WebSocketOpcode.Continuation, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x00, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Continuation, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x00, 0x85, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]

        // Opcode = Ping
        [InlineData(new byte[0], WebSocketOpcode.Ping, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x89, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Ping, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x89, 0x85, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
        [InlineData(new byte[0], WebSocketOpcode.Ping, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x09, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Ping, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x09, 0x85, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]

        // Opcode = Pong
        [InlineData(new byte[0], WebSocketOpcode.Pong, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x8A, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Pong, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x8A, 0x85, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
        [InlineData(new byte[0], WebSocketOpcode.Pong, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x0A, 0x80, 0x01, 0x02, 0x03, 0x04 })]
        [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Pong, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x0A, 0x85, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
        public async Task WriteMaskedBinaryFormattedFrames(byte[] payload, WebSocketOpcode opcode, bool endOfMessage, byte[] maskingKey, byte[] expectedRawFrame)
        {
            var data = await RunSendTest(
                producer: async (socket) =>
                {
                    await socket.SendAsync(CreateFrame(
                        endOfMessage,
                        opcode,
                        payload: payload)).OrTimeout();
                }, options: DefaultTestOptions.WithFixedMaskingKey(maskingKey));
            Assert.Equal(expectedRawFrame, data);
        }

        [Fact]
        public async Task WriteRandomMaskedFrame()
        {
            var data = await RunSendTest(
                producer: async (socket) =>
                {
                    await socket.SendAsync(CreateFrame(
                        endOfMessage: true,
                        opcode: WebSocketOpcode.Binary,
                        payload: new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E })).OrTimeout();
                }, options: DefaultTestOptions.WithRandomMasking());

            // Verify the header
            Assert.Equal(0x82, data[0]);
            Assert.Equal(0x85, data[1]);

            // We don't know the mask, so we have to read it in order to verify this frame
            var mask = data.Slice(2, 4);
            var actualPayload = data.Slice(6);

            // Unmask the payload
            for (int i = 0; i < actualPayload.Length; i++)
            {
                actualPayload[i] = (byte)(mask[i % 4] ^ actualPayload[i]);
            }
            Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E }, actualPayload.ToArray());
        }

        [Theory]
        [InlineData(WebSocketCloseStatus.MandatoryExtension, "Hi", null, new byte[] { 0x88, 0x04, 0x03, 0xF2, (byte)'H', (byte)'i' })]
        [InlineData(WebSocketCloseStatus.PolicyViolation, "", null, new byte[] { 0x88, 0x02, 0x03, 0xF0 })]
        [InlineData(WebSocketCloseStatus.MandatoryExtension, "Hi", new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x88, 0x84, 0x01, 0x02, 0x03, 0x04, 0x03 ^ 0x01, 0xF2 ^ 0x02, (byte)'H' ^ 0x03, (byte)'i' ^ 0x04 })]
        [InlineData(WebSocketCloseStatus.PolicyViolation, "", new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x88, 0x82, 0x01, 0x02, 0x03, 0x04, 0x03 ^ 0x01, 0xF0 ^ 0x02 })]
        public async Task WriteCloseFrames(WebSocketCloseStatus status, string description, byte[] maskingKey, byte[] expectedRawFrame)
        {
            var data = await RunSendTest(
                producer: async (socket) =>
                {
                    await socket.CloseAsync(new WebSocketCloseResult(status, description)).OrTimeout();
                }, options: maskingKey == null ? DefaultTestOptions : DefaultTestOptions.WithFixedMaskingKey(maskingKey));
            Assert.Equal(expectedRawFrame, data);
        }

        private static async Task<byte[]> RunSendTest(Func<WebSocketConnection, Task> producer, WebSocketOptions options)
        {
            using (var factory = new PipeFactory())
            {
                var outbound = factory.Create();
                var inbound = factory.Create();

                using (var connection = new WebSocketConnection(inbound.Reader, outbound.Writer, options))
                {
                    var executeTask = connection.ExecuteAndCaptureFramesAsync();
                    await producer(connection).OrTimeout();
                    connection.Abort();
                    inbound.Writer.Complete();
                    await executeTask.OrTimeout();
                }

                var buffer = await outbound.Reader.ReadToEndAsync();
                var data = buffer.ToArray();
                outbound.Reader.Advance(buffer.End);
                inbound.Reader.Complete();
                CompleteChannels(outbound);
                return data;
            }
        }

        private static void CompleteChannels(params IPipe[] readerWriters)
        {
            foreach (var readerWriter in readerWriters)
            {
                readerWriter.Reader.Complete();
                readerWriter.Writer.Complete();
            }
        }
    }
}
