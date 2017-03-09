// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Tests.Common;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    public partial class WebSocketConnectionTests
    {
        [Theory]
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, "Hello")]
        [InlineData(new byte[] { 0xC2, 0xA7, 0x31, 0x2C, 0x20, 0x39, 0x35, 0xC2, 0xA2 }, "§1, 95¢")]
        [InlineData(new byte[] { 0xE0, 0xA0, 0x80, 0xE0, 0xA4, 0x80 }, "\u0800\u0900")]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80, 0x80 }, "\U00010000")]
        public async Task ValidSingleFramePayloads(byte[] payload, string decoded)
        {
            using (var pair = WebSocketPair.Create())
            {
                var timeoutToken = TestUtil.CreateTimeoutToken();
                using (timeoutToken.Register(() => pair.Dispose()))
                {
                    var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();
                    var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                    var frame = new WebSocketFrame(
                        endOfMessage: true,
                        opcode: WebSocketOpcode.Text,
                        payload: ReadableBuffer.Create(payload));
                    await pair.ClientSocket.SendAsync(frame).OrTimeout();
                    await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure).OrTimeout();
                    var serverSummary = await server.OrTimeout();
                    await pair.ServerSocket.CloseAsync(WebSocketCloseStatus.NormalClosure).OrTimeout();
                    var clientSummary = await client.OrTimeout();

                    Assert.Equal(0, clientSummary.Received.Count);

                    Assert.Equal(1, serverSummary.Received.Count);
                    Assert.True(serverSummary.Received[0].EndOfMessage);
                    Assert.Equal(WebSocketOpcode.Text, serverSummary.Received[0].Opcode);
                    Assert.Equal(decoded, Encoding.UTF8.GetString(serverSummary.Received[0].Payload.ToArray()));
                }
            }
        }

        [Theory]
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0x6C, 0x6C, 0x6F }, "Hello")]

        [InlineData(new byte[0], new byte[] { 0xC2, 0xA7 }, "§")]
        [InlineData(new byte[] { 0xC2 }, new byte[] { 0xA7 }, "§")]
        [InlineData(new byte[] { 0xC2, 0xA7 }, new byte[0], "§")]

        [InlineData(new byte[0], new byte[] { 0xC2, 0xA2 }, "¢")]
        [InlineData(new byte[] { 0xC2 }, new byte[] { 0xA2 }, "¢")]
        [InlineData(new byte[] { 0xC2, 0xA2 }, new byte[0], "¢")]

        [InlineData(new byte[0], new byte[] { 0xE0, 0xA0, 0x80 }, "\u0800")]
        [InlineData(new byte[] { 0xE0 }, new byte[] { 0xA0, 0x80 }, "\u0800")]
        [InlineData(new byte[] { 0xE0, 0xA0 }, new byte[] { 0x80 }, "\u0800")]
        [InlineData(new byte[] { 0xE0, 0xA0, 0x80 }, new byte[0], "\u0800")]

        [InlineData(new byte[0], new byte[] { 0xE0, 0xA4, 0x80 }, "\u0900")]
        [InlineData(new byte[] { 0xE0 }, new byte[] { 0xA4, 0x80 }, "\u0900")]
        [InlineData(new byte[] { 0xE0, 0xA4 }, new byte[] { 0x80 }, "\u0900")]
        [InlineData(new byte[] { 0xE0, 0xA4, 0x80 }, new byte[0], "\u0900")]

        [InlineData(new byte[0], new byte[] { 0xF0, 0x90, 0x80, 0x80 }, "\U00010000")]
        [InlineData(new byte[] { 0xF0 }, new byte[] { 0x90, 0x80, 0x80 }, "\U00010000")]
        [InlineData(new byte[] { 0xF0, 0x90 }, new byte[] { 0x80, 0x80 }, "\U00010000")]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80 }, new byte[] { 0x80 }, "\U00010000")]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80, 0x80 }, new byte[0], "\U00010000")]
        public async Task ValidMultiFramePayloads(byte[] payload1, byte[] payload2, string decoded)
        {
            using (var pair = WebSocketPair.Create())
            {
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                var frame = new WebSocketFrame(
                    endOfMessage: false,
                    opcode: WebSocketOpcode.Text,
                    payload: ReadableBuffer.Create(payload1));
                await pair.ClientSocket.SendAsync(frame).OrTimeout();
                frame = new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Continuation,
                    payload: ReadableBuffer.Create(payload2));
                await pair.ClientSocket.SendAsync(frame).OrTimeout();
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure).OrTimeout();
                var serverSummary = await server.OrTimeout();
                await pair.ServerSocket.CloseAsync(WebSocketCloseStatus.NormalClosure).OrTimeout();
                var clientSummary = await client.OrTimeout();

                Assert.Equal(0, clientSummary.Received.Count);

                Assert.Equal(2, serverSummary.Received.Count);
                Assert.False(serverSummary.Received[0].EndOfMessage);
                Assert.Equal(WebSocketOpcode.Text, serverSummary.Received[0].Opcode);
                Assert.True(serverSummary.Received[1].EndOfMessage);
                Assert.Equal(WebSocketOpcode.Continuation, serverSummary.Received[1].Opcode);

                var finalPayload = serverSummary.Received.SelectMany(f => f.Payload.ToArray()).ToArray();
                Assert.Equal(decoded, Encoding.UTF8.GetString(finalPayload));
            }
        }

        [Theory]

        // Continuation byte as first byte of code point
        [InlineData(new byte[] { 0x48, 0x65, 0x80, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65, 0x99, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65, 0xAB, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65, 0xB0, 0x6C, 0x6F })]

        // Incomplete Code Point
        [InlineData(new byte[] { 0xC2 })]
        [InlineData(new byte[] { 0xE0 })]
        [InlineData(new byte[] { 0xE0, 0xA0 })]
        [InlineData(new byte[] { 0xE0, 0xA4 })]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80 })]

        // Overlong Encoding

        // 'H' (1 byte char) encoded with 2, 3 and 4 bytes
        [InlineData(new byte[] { 0xC1, 0x88 })]
        [InlineData(new byte[] { 0xE0, 0x81, 0x88 })]
        [InlineData(new byte[] { 0xF0, 0x80, 0x81, 0x88 })]

        // '§' (2 byte char) encoded with 3 and 4 bytes
        [InlineData(new byte[] { 0xE0, 0x82, 0xA7 })]
        [InlineData(new byte[] { 0xF0, 0x80, 0x82, 0xA7 })]

        // '\u0800' (3 byte char) encoded with 4 bytes
        [InlineData(new byte[] { 0xF0, 0x80, 0xA0, 0x80 })]
        public async Task InvalidSingleFramePayloads(byte[] payload)
        {
            using (var pair = WebSocketPair.Create())
            {
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                var frame = new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Text,
                    payload: ReadableBuffer.Create(payload));
                await pair.ClientSocket.SendAsync(frame).OrTimeout();
                var clientSummary = await client.OrTimeout();
                var serverSummary = await server.OrTimeout();

                Assert.Equal(0, serverSummary.Received.Count);
                Assert.Equal(0, clientSummary.Received.Count);
                Assert.Equal(WebSocketCloseStatus.InvalidPayloadData, clientSummary.CloseResult.Status);
                Assert.Equal("An invalid Text frame payload was received", clientSummary.CloseResult.Description);
            }
        }

        [Theory]

        // Continuation byte as first byte of code point
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0x80, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0x99, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0xAB, 0x6C, 0x6F })]
        [InlineData(new byte[] { 0x48, 0x65 }, new byte[] { 0xB0, 0x6C, 0x6F })]

        // Incomplete Code Point
        [InlineData(new byte[] { 0xC2 }, new byte[0])]
        [InlineData(new byte[] { 0xE0 }, new byte[0])]
        [InlineData(new byte[] { 0xE0, 0xA0 }, new byte[0])]
        [InlineData(new byte[] { 0xE0, 0xA4 }, new byte[0])]
        [InlineData(new byte[] { 0xF0, 0x90, 0x80 }, new byte[0])]

        // Overlong Encoding

        // 'H' (1 byte char) encoded with 2, 3 and 4 bytes
        [InlineData(new byte[] { 0xC1 }, new byte[] { 0x88 })]
        [InlineData(new byte[] { 0xE0 }, new byte[] { 0x81, 0x88 })]
        [InlineData(new byte[] { 0xF0 }, new byte[] { 0x80, 0x81, 0x88 })]

        // '§' (2 byte char) encoded with 3 and 4 bytes
        [InlineData(new byte[] { 0xE0, 0x82 }, new byte[] { 0xA7 })]
        [InlineData(new byte[] { 0xF0, 0x80 }, new byte[] { 0x82, 0xA7 })]

        // '\u0800' (3 byte char) encoded with 4 bytes
        [InlineData(new byte[] { 0xF0, 0x80 }, new byte[] { 0xA0, 0x80 })]
        public async Task InvalidMultiFramePayloads(byte[] payload1, byte[] payload2)
        {
            using (var pair = WebSocketPair.Create())
            {
                var timeoutToken = TestUtil.CreateTimeoutToken();
                using (timeoutToken.Register(() => pair.Dispose()))
                {
                    var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();
                    var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                    var frame = new WebSocketFrame(
                        endOfMessage: false,
                        opcode: WebSocketOpcode.Text,
                        payload: ReadableBuffer.Create(payload1));
                    await pair.ClientSocket.SendAsync(frame).OrTimeout();
                    frame = new WebSocketFrame(
                        endOfMessage: true,
                        opcode: WebSocketOpcode.Continuation,
                        payload: ReadableBuffer.Create(payload2));
                    await pair.ClientSocket.SendAsync(frame).OrTimeout();
                    var clientSummary = await client.OrTimeout();
                    var serverSummary = await server.OrTimeout();

                    Assert.Equal(1, serverSummary.Received.Count);
                    Assert.False(serverSummary.Received[0].EndOfMessage);
                    Assert.Equal(WebSocketOpcode.Text, serverSummary.Received[0].Opcode);
                    Assert.Equal(payload1, serverSummary.Received[0].Payload.ToArray());

                    Assert.Equal(0, clientSummary.Received.Count);
                    Assert.Equal(WebSocketCloseStatus.InvalidPayloadData, clientSummary.CloseResult.Status);
                    Assert.Equal("An invalid Text frame payload was received", clientSummary.CloseResult.Description);
                }
            }
        }
    }
}
