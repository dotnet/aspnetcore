using System;
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
        public class TheSendAsyncMethod
        {
            [Theory]
            [InlineData("", true, new byte[] { 0x11, 0x00 })]
            [InlineData("Hello", true, new byte[] { 0x11, 0x0A, 0x48, 0x65, 0x6C, 0x6C, 0x6F })]
            [InlineData("", false, new byte[] { 0x10, 0x00 })]
            [InlineData("Hello", false, new byte[] { 0x10, 0x0A, 0x48, 0x65, 0x6C, 0x6C, 0x6F })]
            public async Task WriteTextFrames(string message, bool endOfMessage, byte[] expectedRawFrame)
            {
                var data = await RunSendTest(
                    producer: async (socket, cancellationToken) =>
                    {
                        var payload = Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(CreateFrame(
                            endOfMessage,
                            opcode: WebSocketOpcode.Text,
                            payload: payload));
                    }, masked: false);
                Assert.Equal(expectedRawFrame, data);
            }

            [Theory]
            // Opcode = Binary
            [InlineData(new byte[0], WebSocketOpcode.Binary, true, new byte[] { 0x21, 0x00 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Binary, true, new byte[] { 0x21, 0x0A, 0xA, 0xB, 0xC, 0xD, 0xE })]
            [InlineData(new byte[0], WebSocketOpcode.Binary, false, new byte[] { 0x20, 0x00 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Binary, false, new byte[] { 0x20, 0x0A, 0xA, 0xB, 0xC, 0xD, 0xE })]

            // Opcode = Continuation
            [InlineData(new byte[0], WebSocketOpcode.Continuation, true, new byte[] { 0x01, 0x00 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Continuation, true, new byte[] { 0x01, 0x0A, 0xA, 0xB, 0xC, 0xD, 0xE })]
            [InlineData(new byte[0], WebSocketOpcode.Continuation, false, new byte[] { 0x00, 0x00 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Continuation, false, new byte[] { 0x00, 0x0A, 0xA, 0xB, 0xC, 0xD, 0xE })]

            // Opcode = Ping
            [InlineData(new byte[0], WebSocketOpcode.Ping, true, new byte[] { 0x91, 0x00 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Ping, true, new byte[] { 0x91, 0x0A, 0xA, 0xB, 0xC, 0xD, 0xE })]
            [InlineData(new byte[0], WebSocketOpcode.Ping, false, new byte[] { 0x90, 0x00 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Ping, false, new byte[] { 0x90, 0x0A, 0xA, 0xB, 0xC, 0xD, 0xE })]

            // Opcode = Pong
            [InlineData(new byte[0], WebSocketOpcode.Pong, true, new byte[] { 0xA1, 0x00 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Pong, true, new byte[] { 0xA1, 0x0A, 0xA, 0xB, 0xC, 0xD, 0xE })]
            [InlineData(new byte[0], WebSocketOpcode.Pong, false, new byte[] { 0xA0, 0x00 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Pong, false, new byte[] { 0xA0, 0x0A, 0xA, 0xB, 0xC, 0xD, 0xE })]
            public async Task WriteBinaryFormattedFrames(byte[] payload, WebSocketOpcode opcode, bool endOfMessage, byte[] expectedRawFrame)
            {
                var data = await RunSendTest(
                    producer: async (socket, cancellationToken) =>
                    {
                        await socket.SendAsync(CreateFrame(
                            endOfMessage,
                            opcode,
                            payload: payload));
                    }, masked: false);
                Assert.Equal(expectedRawFrame, data);
            }

            [Theory]
            [InlineData("", new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x11, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData("Hello", new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x11, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x48 ^ 0x01, 0x65 ^ 0x02, 0x6C ^ 0x03, 0x6C ^ 0x04, 0x6F ^ 0x01 })]
            public async Task WriteMaskedTextFrames(string message, byte[] maskingKey, byte[] expectedRawFrame)
            {
                var data = await RunSendTest(
                    producer: async (socket, cancellationToken) =>
                    {
                        var payload = Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(CreateFrame(
                            endOfMessage: true,
                            opcode: WebSocketOpcode.Text,
                            payload: payload));
                    }, maskingKey: maskingKey);
                Assert.Equal(expectedRawFrame, data);
            }

            [Theory]
            // Opcode = Binary
            [InlineData(new byte[0], WebSocketOpcode.Binary, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x21, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Binary, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x21, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
            [InlineData(new byte[0], WebSocketOpcode.Binary, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x20, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Binary, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x20, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]

            // Opcode = Continuation
            [InlineData(new byte[0], WebSocketOpcode.Continuation, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x01, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Continuation, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x01, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
            [InlineData(new byte[0], WebSocketOpcode.Continuation, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x00, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Continuation, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x00, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]

            // Opcode = Ping
            [InlineData(new byte[0], WebSocketOpcode.Ping, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x91, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Ping, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x91, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
            [InlineData(new byte[0], WebSocketOpcode.Ping, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x90, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Ping, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x90, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]

            // Opcode = Pong
            [InlineData(new byte[0], WebSocketOpcode.Pong, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0xA1, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Pong, true, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0xA1, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
            [InlineData(new byte[0], WebSocketOpcode.Pong, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0xA0, 0x01, 0x01, 0x02, 0x03, 0x04 })]
            [InlineData(new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE }, WebSocketOpcode.Pong, false, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0xA0, 0x0B, 0x01, 0x02, 0x03, 0x04, 0x0A ^ 0x01, 0x0B ^ 0x02, 0x0C ^ 0x03, 0x0D ^ 0x04, 0x0E ^ 0x01 })]
            public async Task WriteMaskedBinaryFormattedFrames(byte[] payload, WebSocketOpcode opcode, bool endOfMessage, byte[] maskingKey, byte[] expectedRawFrame)
            {
                var data = await RunSendTest(
                    producer: async (socket, cancellationToken) =>
                    {
                        await socket.SendAsync(CreateFrame(
                            endOfMessage,
                            opcode,
                            payload: payload));
                    }, maskingKey: maskingKey);
                Assert.Equal(expectedRawFrame, data);
            }

            [Fact]
            public async Task WriteRandomMaskedFrame()
            {
                var data = await RunSendTest(
                    producer: async (socket, cancellationToken) =>
                    {
                        await socket.SendAsync(CreateFrame(
                            endOfMessage: true,
                            opcode: WebSocketOpcode.Binary,
                            payload: new byte[] { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E }));
                    }, masked: true);

                // Verify the header
                Assert.Equal(0x21, data[0]);
                Assert.Equal(0x0B, data[1]);

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
            [InlineData(WebSocketCloseStatus.MandatoryExtension, "Hi", null, new byte[] { 0x81, 0x08, 0x03, 0xF2, (byte)'H', (byte)'i' })]
            [InlineData(WebSocketCloseStatus.PolicyViolation, "", null, new byte[] { 0x81, 0x04, 0x03, 0xF0 })]
            [InlineData(WebSocketCloseStatus.MandatoryExtension, "Hi", new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x81, 0x09, 0x01, 0x02, 0x03, 0x04, 0x03 ^ 0x01, 0xF2 ^ 0x02, (byte)'H' ^ 0x03, (byte)'i' ^ 0x04 })]
            [InlineData(WebSocketCloseStatus.PolicyViolation, "", new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x81, 0x05, 0x01, 0x02, 0x03, 0x04, 0x03 ^ 0x01, 0xF0 ^ 0x02 })]
            public async Task WriteCloseFrames(WebSocketCloseStatus status, string description, byte[] maskingKey, byte[] expectedRawFrame)
            {
                var data = await RunSendTest(
                    producer: async (socket, cancellationToken) =>
                    {
                        await socket.CloseAsync(new WebSocketCloseResult(status, description));
                    }, maskingKey: maskingKey);
                Assert.Equal(expectedRawFrame, data);
            }

            private static async Task<byte[]> RunSendTest(Func<WebSocketConnection, CancellationToken, Task> producer, bool masked = false, byte[] maskingKey = null)
            {
                using (var factory = new ChannelFactory())
                {
                    var outbound = factory.CreateChannel();
                    var inbound = factory.CreateChannel();

                    var cts = new CancellationTokenSource();

                    // Timeout for the test, but only if the debugger is not attached.
                    if (!Debugger.IsAttached)
                    {
                        cts.CancelAfter(TimeSpan.FromSeconds(5));
                    }

                    var cancellationToken = cts.Token;
                    using (cancellationToken.Register(() => CompleteChannels(inbound, outbound)))
                    {

                        Task executeTask;
                        using (var connection = CreateConnection(inbound, outbound, masked, maskingKey))
                        {
                            executeTask = connection.ExecuteAsync(f =>
                            {
                                Assert.False(true, "Did not expect to receive any messages");
                                return Task.CompletedTask;
                            });
                            await producer(connection, cancellationToken);
                            inbound.CompleteWriter();
                            await executeTask;
                        }

                        var data = (await outbound.ReadToEndAsync()).ToArray();
                        inbound.CompleteReader();
                        CompleteChannels(outbound);
                        return data;
                    }
                }
            }

            private static void CompleteChannels(params Channel[] channels)
            {
                foreach (var channel in channels)
                {
                    channel.CompleteReader();
                    channel.CompleteWriter();
                }
            }

            private static WebSocketConnection CreateConnection(Channel inbound, Channel outbound, bool masked, byte[] maskingKey)
            {
                return (maskingKey != null) ?
                    new WebSocketConnection(inbound, outbound, fixedMaskingKey: maskingKey) :
                    new WebSocketConnection(inbound, outbound, masked);
            }
        }
    }
}
