using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Channels;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Tests
{
    public partial class WebSocketConnectionTests
    {
        public class TheReceiveAsyncMethod
        {
            [Theory]
            [InlineData(new byte[] { 0x11, 0x00 }, "", true)]
            [InlineData(new byte[] { 0x11, 0x0A, 0x48, 0x65, 0x6C, 0x6C, 0x6F }, "Hello", true)]
            [InlineData(new byte[] { 0x11, 0x0B, 0x1, 0x2, 0x3, 0x4, 0x48 ^ 0x1, 0x65 ^ 0x2, 0x6C ^ 0x3, 0x6C ^ 0x4, 0x6F ^ 0x1 }, "Hello", true)]
            [InlineData(new byte[] { 0x10, 0x00 }, "", false)]
            [InlineData(new byte[] { 0x10, 0x0A, 0x48, 0x65, 0x6C, 0x6C, 0x6F }, "Hello", false)]
            [InlineData(new byte[] { 0x10, 0x0B, 0x1, 0x2, 0x3, 0x4, 0x48 ^ 0x1, 0x65 ^ 0x2, 0x6C ^ 0x3, 0x6C ^ 0x4, 0x6F ^ 0x1 }, "Hello", false)]
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
            [InlineData(new byte[] { 0x21, 0x00 }, new byte[0], WebSocketOpcode.Binary, true)]
            [InlineData(new byte[] { 0x21, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Binary, true)]
            [InlineData(new byte[] { 0x21, 0x0B, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Binary, true)]
            [InlineData(new byte[] { 0x20, 0x00 }, new byte[0], WebSocketOpcode.Binary, false)]
            [InlineData(new byte[] { 0x20, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Binary, false)]
            [InlineData(new byte[] { 0x20, 0x0B, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Binary, false)]

            // Opcode = Continuation
            [InlineData(new byte[] { 0x01, 0x00 }, new byte[0], WebSocketOpcode.Continuation, true)]
            [InlineData(new byte[] { 0x01, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Continuation, true)]
            [InlineData(new byte[] { 0x01, 0x0B, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Continuation, true)]
            [InlineData(new byte[] { 0x00, 0x00 }, new byte[0], WebSocketOpcode.Continuation, false)]
            [InlineData(new byte[] { 0x00, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Continuation, false)]
            [InlineData(new byte[] { 0x00, 0x0B, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Continuation, false)]

            // Opcode = Ping
            [InlineData(new byte[] { 0x91, 0x00 }, new byte[0], WebSocketOpcode.Ping, true)]
            [InlineData(new byte[] { 0x91, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Ping, true)]
            [InlineData(new byte[] { 0x91, 0x0B, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Ping, true)]
            [InlineData(new byte[] { 0x90, 0x00 }, new byte[0], WebSocketOpcode.Ping, false)]
            [InlineData(new byte[] { 0x90, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Ping, false)]
            [InlineData(new byte[] { 0x90, 0x0B, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Ping, false)]

            // Opcode = Pong
            [InlineData(new byte[] { 0xA1, 0x00 }, new byte[0], WebSocketOpcode.Pong, true)]
            [InlineData(new byte[] { 0xA1, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Pong, true)]
            [InlineData(new byte[] { 0xA1, 0x0B, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Pong, true)]
            [InlineData(new byte[] { 0xA0, 0x00 }, new byte[0], WebSocketOpcode.Pong, false)]
            [InlineData(new byte[] { 0xA0, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Pong, false)]
            [InlineData(new byte[] { 0xA0, 0x0B, 0x1, 0x2, 0x3, 0x4, 0xDE ^ 0x1, 0xAD ^ 0x2, 0xBE ^ 0x3, 0xEF ^ 0x4, 0xAB ^ 0x1 }, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB }, WebSocketOpcode.Pong, false)]
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
                        await channel.WriteAsync(new byte[] { 0x20, 0x0A }.Slice());
                        await channel.WriteAsync(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xAB, 0x01, 0x0A }.Slice());
                        await channel.WriteAsync(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }.Slice());
                        await channel.WriteAsync(new byte[] { 0xAB }.Slice());
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
            public async Task Read16BitPayloadLength()
            {
                var expectedPayload = new byte[1024];
                new Random().NextBytes(expectedPayload);

                var result = await RunReceiveTest(
                    producer: async (channel, cancellationToken) =>
                    {
                        // Header: (Opcode=Binary, Fin=true), (Mask=false, Len=126), (16-bit big endian length)
                        await channel.WriteAsync(new byte[] { 0x21, 0xFC, 0x04, 0x00 });
                        await channel.WriteAsync(expectedPayload);
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
                        await channel.WriteAsync(new byte[] { 0x21, 0xFE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00 });
                        await channel.WriteAsync(expectedPayload);
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
                        await channel.WriteAsync(rawFrame.Slice());
                    });
                var frames = result.Received;
                Assert.Equal(1, frames.Count);

                var frame = frames[0];

                Assert.Equal(endOfMessage, frame.EndOfMessage);
                Assert.Equal(expectedOpcode, frame.Opcode);
                payloadAssert(frame.Payload.ToArray());
            }

            private static async Task<WebSocketConnectionSummary> RunReceiveTest(Func<IWritableChannel, CancellationToken, Task> producer)
            {
                using (var factory = new ChannelFactory())
                {
                    var outbound = factory.CreateChannel();
                    var inbound = factory.CreateChannel();

                    var cts = new CancellationTokenSource();
                    var cancellationToken = cts.Token;

                    // Timeout for the test, but only if the debugger is not attached.
                    if (!Debugger.IsAttached)
                    {
                        cts.CancelAfter(TimeSpan.FromSeconds(5));
                    }

                    var producerTask = Task.Run(async () =>
                    {
                        await producer(inbound, cancellationToken);
                        inbound.CompleteWriter();
                    }, cancellationToken);

                    var consumerTask = Task.Run(async () =>
                    {
                        var connection = new WebSocketConnection(inbound, outbound);
                        using (cancellationToken.Register(() => connection.Dispose()))
                        using (connection)
                        {
                            // Receive frames until we're closed
                            return await connection.ExecuteAndCaptureFramesAsync();
                        }
                    }, cancellationToken);

                    await Task.WhenAll(producerTask, consumerTask);
                    return consumerTask.Result;
                }
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
