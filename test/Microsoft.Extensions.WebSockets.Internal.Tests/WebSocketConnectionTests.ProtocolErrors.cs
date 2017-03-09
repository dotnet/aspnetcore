// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Tests.Common;
using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    public partial class WebSocketConnectionTests
    {
        [Theory]
        [InlineData(new byte[] { 0x11, 0x00 })]
        [InlineData(new byte[] { 0x21, 0x00 })]
        [InlineData(new byte[] { 0x31, 0x00 })]
        [InlineData(new byte[] { 0x41, 0x00 })]
        [InlineData(new byte[] { 0x51, 0x00 })]
        [InlineData(new byte[] { 0x61, 0x00 })]
        [InlineData(new byte[] { 0x71, 0x00 })]
        public Task TerminatesConnectionOnReservedBitSet(byte[] rawFrame)
        {
            return WriteFrameAndExpectClose(rawFrame, WebSocketCloseStatus.ProtocolError, "Reserved bits, which are required to be zero, were set.");
        }

        [Theory]
        [InlineData(0x03)]
        [InlineData(0x04)]
        [InlineData(0x05)]
        [InlineData(0x06)]
        [InlineData(0x07)]
        [InlineData(0x0B)]
        [InlineData(0x0C)]
        [InlineData(0x0D)]
        [InlineData(0x0E)]
        [InlineData(0x0F)]
        public Task ReservedOpcodes(byte opcode)
        {
            var payload = Encoding.UTF8.GetBytes("hello");
            var frame = new WebSocketFrame(
                    endOfMessage: true,
                    opcode: (WebSocketOpcode)opcode,
                    payload: ReadableBuffer.Create(payload));
            return SendFrameAndExpectClose(frame, WebSocketCloseStatus.ProtocolError, $"Received frame using reserved opcode: 0x{opcode:X}");
        }

        [Theory]
        [InlineData(new byte[] { 0x88, 0x01, 0xAB })]

        // Invalid UTF-8 reason
        [InlineData(new byte[] { 0x88, 0x07, 0x03, 0xE8, 0x48, 0x65, 0x80, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x88, 0x07, 0x03, 0xE8, 0x48, 0x65, 0x99, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x88, 0x07, 0x03, 0xE8, 0x48, 0x65, 0xAB, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x88, 0x07, 0x03, 0xE8, 0x48, 0x65, 0xB0, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x88, 0x03, 0x03, 0xE8, 0xC2 })]
        [InlineData(new byte[] { 0x88, 0x03, 0x03, 0xE8, 0xE0 })]
        [InlineData(new byte[] { 0x88, 0x04, 0x03, 0xE8, 0xE0, 0xA0 })]
        [InlineData(new byte[] { 0x88, 0x04, 0x03, 0xE8, 0xE0, 0xA4 })]
        [InlineData(new byte[] { 0x88, 0x05, 0x03, 0xE8, 0xF0, 0x90, 0x80 })]
        [InlineData(new byte[] { 0x88, 0x04, 0x03, 0xE8, 0xC1, 0x88 })]
        [InlineData(new byte[] { 0x88, 0x05, 0x03, 0xE8, 0xE0, 0x81, 0x88 })]
        [InlineData(new byte[] { 0x88, 0x06, 0x03, 0xE8, 0xF0, 0x80, 0x81, 0x88 })]
        [InlineData(new byte[] { 0x88, 0x05, 0x03, 0xE8, 0xE0, 0x82, 0xA7 })]
        [InlineData(new byte[] { 0x88, 0x06, 0x03, 0xE8, 0xF0, 0x80, 0x82, 0xA7 })]
        [InlineData(new byte[] { 0x88, 0x06, 0x03, 0xE8, 0xF0, 0x80, 0xA0, 0x80 })]
        public Task InvalidCloseFrames(byte[] rawFrame)
        {
            return WriteFrameAndExpectClose(rawFrame, WebSocketCloseStatus.ProtocolError, "Close frame payload invalid");
        }

        [Fact]
        public Task CloseFrameTooLong()
        {
            var rawFrame = new byte[256];
            new Random().NextBytes(rawFrame);

            // Put a WebSocket frame header in front
            rawFrame[0] = 0x88; // Close frame, FIN=true
            rawFrame[1] = 0x7E; // Mask=false, LEN=126
            rawFrame[2] = 0x00; // Extended Len = 252 (256 - 4 bytes for header)
            rawFrame[3] = 0xFC;

            return WriteFrameAndExpectClose(rawFrame, WebSocketCloseStatus.ProtocolError, "Close frame payload too long. Maximum size is 125 bytes");
        }

        [Theory]
        // 0-999 reserved
        [InlineData(0)]
        [InlineData(999)]
        // Specifically reserved status codes, or codes that should not be sent in frames.
        [InlineData(1004)]
        [InlineData(1005)]
        [InlineData(1006)]
        [InlineData(1012)]
        [InlineData(1013)]
        [InlineData(1014)]
        [InlineData(1015)]
        // Undefined status codes
        [InlineData(1016)]
        [InlineData(1100)]
        [InlineData(2000)]
        [InlineData(2999)]
        public Task InvalidCloseStatuses(ushort status)
        {
            var rawFrame = new byte[] { 0x88, 0x02, (byte)(status >> 8), (byte)(status) };
            return WriteFrameAndExpectClose(rawFrame, WebSocketCloseStatus.ProtocolError, $"Invalid close status: {status}.");
        }

        [Theory]
        [InlineData(new byte[] { 0x08, 0x00 })]
        [InlineData(new byte[] { 0x09, 0x00 })]
        [InlineData(new byte[] { 0x0A, 0x00 })]
        public Task TerminatesConnectionOnFragmentedControlFrame(byte[] rawFrame)
        {
            return WriteFrameAndExpectClose(rawFrame, WebSocketCloseStatus.ProtocolError, "Control frames may not be fragmented");
        }

        [Fact]
        public async Task TerminatesConnectionOnNonContinuationFrameFollowingFragmentedMessageStart()
        {
            // Arrange
            using (var pair = WebSocketPair.Create(
                serverOptions: new WebSocketOptions().WithAllFramesPassedThrough(),
                clientOptions: new WebSocketOptions().WithAllFramesPassedThrough()))
            {
                var payload = Encoding.UTF8.GetBytes("hello");

                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();

                // Act
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: false,
                    opcode: WebSocketOpcode.Text,
                    payload: ReadableBuffer.Create(payload)));
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Text,
                    payload: ReadableBuffer.Create(payload)));

                // Server should terminate
                var clientSummary = await client.OrTimeout();

                Assert.Equal(WebSocketCloseStatus.ProtocolError, clientSummary.CloseResult.Status);
                Assert.Equal("Received non-continuation frame during a fragmented message", clientSummary.CloseResult.Description);

                await server.OrTimeout();
            }
        }

        [Fact]
        public async Task TerminatesConnectionOnUnsolicitedContinuationFrame()
        {
            // Arrange
            using (var pair = WebSocketPair.Create(
                serverOptions: new WebSocketOptions().WithAllFramesPassedThrough(),
                clientOptions: new WebSocketOptions().WithAllFramesPassedThrough()))
            {
                var payload = Encoding.UTF8.GetBytes("hello");

                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();

                // Act
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Text,
                    payload: ReadableBuffer.Create(payload)));
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Continuation,
                    payload: ReadableBuffer.Create(payload)));

                // Server should terminate
                var clientSummary = await client.OrTimeout();

                Assert.Equal(WebSocketCloseStatus.ProtocolError, clientSummary.CloseResult.Status);
                Assert.Equal("Continuation Frame was received when expecting a new message", clientSummary.CloseResult.Description);

                await server.OrTimeout();
            }
        }

        [Fact]
        public Task TerminatesConnectionOnPingFrameLargerThan125Bytes()
        {
            var payload = new byte[126];
            new Random().NextBytes(payload);
            return SendFrameAndExpectClose(
                new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Ping,
                    payload: ReadableBuffer.Create(payload)),
                WebSocketCloseStatus.ProtocolError,
                "Ping frame exceeded maximum size of 125 bytes");
        }

        private static async Task SendFrameAndExpectClose(WebSocketFrame frame, WebSocketCloseStatus closeStatus, string closeReason)
        {
            // Arrange
            using (var pair = WebSocketPair.Create(
                serverOptions: new WebSocketOptions().WithAllFramesPassedThrough(),
                clientOptions: new WebSocketOptions().WithAllFramesPassedThrough()))
            {
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();

                // Act
                await pair.ClientSocket.SendAsync(frame);

                // Server should terminate
                var clientSummary = await client.OrTimeout();

                Assert.Equal(closeStatus, clientSummary.CloseResult.Status);
                Assert.Equal(closeReason, clientSummary.CloseResult.Description);

                await server.OrTimeout();
            }
        }

        private static async Task WriteFrameAndExpectClose(byte[] rawFrame, WebSocketCloseStatus closeStatus, string closeReason)
        {
            // Arrange
            using (var pair = WebSocketPair.Create(
                serverOptions: new WebSocketOptions().WithAllFramesPassedThrough(),
                clientOptions: new WebSocketOptions().WithAllFramesPassedThrough()))
            {
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();

                // Act
                await pair.ClientToServer.Writer.WriteAsync(rawFrame);

                // Server should terminate
                var clientSummary = await client.OrTimeout();

                Assert.Equal(closeStatus, clientSummary.CloseResult.Status);
                Assert.Equal(closeReason, clientSummary.CloseResult.Description);

                await server.OrTimeout();
            }
        }
    }
}
