using System;
using System.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Channels;
using Microsoft.Extensions.WebSockets.Internal;

namespace Microsoft.Extensions.WebSockets
{
    /// <summary>
    /// Provides the default implementation of <see cref="IWebSocketConnection"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is thread-safe under the following condition: No two threads attempt to call either
    /// <see cref="ReceiveAsync"/> or <see cref="SendAsync"/> simultaneously. Different threads may call each method, but the same method
    /// cannot be re-entered while it is being run in a different thread.
    /// </para>
    /// <para>
    /// The general pattern of having a single thread running <see cref="ReceiveAsync"/> and a separate thread running <see cref="SendAsync"/> will
    /// be thread-safe, as each method interacts with completely separate state.
    /// </para>
    /// </remarks>
    public class WebSocketConnection : IWebSocketConnection
    {
        private readonly RandomNumberGenerator _random;
        private readonly byte[] _maskingKey;
        private readonly IReadableChannel _inbound;
        private readonly IWritableChannel _outbound;
        private readonly CancellationTokenSource _terminateReceiveCts = new CancellationTokenSource();

        public WebSocketConnectionState State { get; private set; } = WebSocketConnectionState.Created;

        /// <summary>
        /// Constructs a new, unmasked, <see cref="WebSocketConnection"/> from an <see cref="IReadableChannel"/> and an <see cref="IWritableChannel"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IReadableChannel"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IWritableChannel"/> to which frame will be written when sending.</param>
        public WebSocketConnection(IReadableChannel inbound, IWritableChannel outbound) : this(inbound, outbound, masked: false) { }

        /// <summary>
        /// Constructs a new, optionally masked, <see cref="WebSocketConnection"/> from an <see cref="IReadableChannel"/> and an <see cref="IWritableChannel"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IReadableChannel"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IWritableChannel"/> to which frame will be written when sending.</param>
        /// <param name="masked">A boolean indicating if frames sent from this socket should be masked (the masking key is automatically generated)</param>
        public WebSocketConnection(IReadableChannel inbound, IWritableChannel outbound, bool masked)
        {
            _inbound = inbound;
            _outbound = outbound;

            if (masked)
            {
                _maskingKey = new byte[4];
                _random = RandomNumberGenerator.Create();
            }
        }

        /// <summary>
        /// Constructs a new, fixed masking-key, <see cref="WebSocketConnection"/> from an <see cref="IReadableChannel"/> and an <see cref="IWritableChannel"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IReadableChannel"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IWritableChannel"/> to which frame will be written when sending.</param>
        /// <param name="fixedMaskingKey">The masking key to use for the connection. Must be exactly 4-bytes long. This is ONLY recommended for testing and development purposes.</param>
        public WebSocketConnection(IReadableChannel inbound, IWritableChannel outbound, byte[] fixedMaskingKey)
        {
            _inbound = inbound;
            _outbound = outbound;
            _maskingKey = fixedMaskingKey;
        }

        public void Dispose()
        {
            State = WebSocketConnectionState.Closed;
            _inbound.Complete();
            _outbound.Complete();
            _terminateReceiveCts.Cancel();
        }

        public Task<WebSocketCloseResult> ExecuteAsync(Func<WebSocketFrame, Task> messageHandler)
        {
            if (State == WebSocketConnectionState.Closed)
            {
                throw new ObjectDisposedException(nameof(WebSocketConnection));
            }

            if (State != WebSocketConnectionState.Created)
            {
                throw new InvalidOperationException("Connection is already running.");
            }
            State = WebSocketConnectionState.Connected;
            return Task.Run(() => ReceiveLoop(messageHandler, _terminateReceiveCts.Token));
        }

        /// <summary>
        /// Sends the specified frame.
        /// </summary>
        /// <param name="frame">The frame to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the message has been written to the outbound stream.</returns>
        // TODO: De-taskify this to allow consumers to create their own awaiter.
        public Task SendAsync(WebSocketFrame frame, CancellationToken cancellationToken)
        {
            if (State == WebSocketConnectionState.Closed)
            {
                throw new ObjectDisposedException(nameof(WebSocketConnection));
            }
            // This clause is a bit of an artificial restriction to ensure people run "Execute". Maybe we don't care?
            else if (State == WebSocketConnectionState.Created)
            {
                throw new InvalidOperationException("Cannot send until the connection is started using Execute");
            }
            else if (State == WebSocketConnectionState.CloseSent)
            {
                throw new InvalidOperationException("Cannot send after sending a Close frame");
            }

            if (frame.Opcode == WebSocketOpcode.Close)
            {
                throw new InvalidOperationException("Cannot use SendAsync to send a Close frame, use CloseAsync instead.");
            }
            return SendCoreAsync(frame, null, cancellationToken);
        }

        /// <summary>
        /// Sends a Close frame to the other party. This does not guarantee that the client will send a responding close frame.
        /// </summary>
        /// <remarks>
        /// If the other party does not respond with a close frame, the connection will remain open and the <see cref="Task{WebSocketCloseResult}"/>
        /// will remain active. Call the <see cref="IDisposable.Dispose"/> method on this instance to forcibly terminate the connection.
        /// </remarks>
        /// <param name="result">A <see cref="WebSocketCloseResult"/> with the payload for the close frame</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the close frame has been sent</returns>
        public async Task CloseAsync(WebSocketCloseResult result, CancellationToken cancellationToken)
        {
            if (State == WebSocketConnectionState.Closed)
            {
                throw new ObjectDisposedException(nameof(WebSocketConnection));
            }
            else if (State == WebSocketConnectionState.Created)
            {
                throw new InvalidOperationException("Cannot send close frame when the connection hasn't been started");
            }
            else if (State == WebSocketConnectionState.CloseSent)
            {
                throw new InvalidOperationException("Cannot send multiple close frames");
            }

            // When we pass a close result to SendCoreAsync, the frame is only used for the header and the payload is ignored
            var frame = new WebSocketFrame(endOfMessage: true, opcode: WebSocketOpcode.Close, payload: default(ReadableBuffer));

            await SendCoreAsync(frame, result, cancellationToken);

            if (State == WebSocketConnectionState.CloseReceived)
            {
                State = WebSocketConnectionState.Closed;
            }
            else
            {
                State = WebSocketConnectionState.CloseSent;
            }
        }

        private void WriteMaskingKey(Span<byte> buffer)
        {
            if (_random != null)
            {
                // Get a new random mask
                // Until https://github.com/dotnet/corefx/issues/12323 is fixed we need to use this shared buffer and copy model
                // Once we have that fix we should be able to generate the mask directly into the output buffer.
                _random.GetBytes(_maskingKey);
            }

            buffer.Set(_maskingKey);
        }

        private async Task<WebSocketCloseResult> ReceiveLoop(Func<WebSocketFrame, Task> messageHandler, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // WebSocket Frame layout (https://tools.ietf.org/html/rfc6455#section-5.2):
                //  0                   1                   2                   3
                //  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                // +-+-+-+-+-------+-+-------------+-------------------------------+
                // |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
                // |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
                // |N|V|V|V|       |S|             |   (if payload len==126/127)   |
                // | |1|2|3|       |K|             |                               |
                // +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
                // |     Extended payload length continued, if payload len == 127  |
                // + - - - - - - - - - - - - - - - +-------------------------------+
                // |                               |Masking-key, if MASK set to 1  |
                // +-------------------------------+-------------------------------+
                // | Masking-key (continued)       |          Payload Data         |
                // +-------------------------------- - - - - - - - - - - - - - - - +
                // :                     Payload Data continued ...                :
                // + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
                // |                     Payload Data continued ...                |
                // +---------------------------------------------------------------+

                // Read at least 2 bytes
                var result = await _inbound.ReadAtLeastAsync(2, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (result.IsCompleted && result.Buffer.Length < 2)
                {
                    return WebSocketCloseResult.AbnormalClosure;
                }
                var buffer = result.Buffer;

                // Read the opcode
                var opcodeByte = buffer.ReadBigEndian<byte>();
                buffer = buffer.Slice(1);

                var fin = (opcodeByte & 0x01) != 0;
                var opcode = (WebSocketOpcode)((opcodeByte & 0xF0) >> 4);

                // Read the first byte of the payload length
                var lenByte = buffer.ReadBigEndian<byte>();
                buffer = buffer.Slice(1);

                var masked = (lenByte & 0x01) != 0;
                var payloadLen = (lenByte & 0xFE) >> 1;

                // Mark what we've got so far as consumed
                _inbound.Advance(buffer.Start);

                // Calculate the rest of the header length
                var headerLength = masked ? 4 : 0;
                if (payloadLen == 126)
                {
                    headerLength += 2;
                }
                else if (payloadLen == 127)
                {
                    headerLength += 4;
                }

                uint maskingKey = 0;

                if (headerLength > 0)
                {
                    result = await _inbound.ReadAtLeastAsync(headerLength, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (result.IsCompleted && result.Buffer.Length < headerLength)
                    {
                        return WebSocketCloseResult.AbnormalClosure;
                    }
                    buffer = result.Buffer;

                    // Read extended payload length (if any)
                    if (payloadLen == 126)
                    {
                        payloadLen = buffer.ReadBigEndian<ushort>();
                        buffer = buffer.Slice(sizeof(ushort));
                    }
                    else if (payloadLen == 127)
                    {
                        var longLen = buffer.ReadBigEndian<ulong>();
                        buffer = buffer.Slice(sizeof(ulong));
                        if (longLen > int.MaxValue)
                        {
                            throw new WebSocketException($"Frame is too large. Maximum frame size is {int.MaxValue} bytes");
                        }
                        payloadLen = (int)longLen;
                    }

                    // Read masking key
                    if (masked)
                    {
                        var maskingKeyStart = buffer.Start;
                        maskingKey = buffer.Slice(0, 4).ReadBigEndian<uint>();
                        buffer = buffer.Slice(4);
                    }

                    // Mark the length and masking key consumed
                    _inbound.Advance(buffer.Start);
                }

                var payload = default(ReadableBuffer);
                if (payloadLen > 0)
                {
                    result = await _inbound.ReadAtLeastAsync(payloadLen, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (result.IsCompleted && result.Buffer.Length < payloadLen)
                    {
                        return WebSocketCloseResult.AbnormalClosure;
                    }
                    buffer = result.Buffer;

                    payload = buffer.Slice(0, payloadLen);

                    if (masked)
                    {
                        // Unmask
                        MaskingUtilities.ApplyMask(ref payload, maskingKey);
                    }
                }

                // Run the callback, if we're not cancelled.
                cancellationToken.ThrowIfCancellationRequested();

                var frame = new WebSocketFrame(fin, opcode, payload);
                if (frame.Opcode == WebSocketOpcode.Close)
                {
                    return HandleCloseFrame(payloadLen, payload, frame);
                }
                else
                {
                    await messageHandler(frame);
                }

                // Mark the payload as consumed
                if (payloadLen > 0)
                {
                    _inbound.Advance(payload.End);
                }
            }
            return WebSocketCloseResult.AbnormalClosure;
        }

        private WebSocketCloseResult HandleCloseFrame(int payloadLen, ReadableBuffer payload, WebSocketFrame frame)
        {
            // Update state
            if (State == WebSocketConnectionState.CloseSent)
            {
                State = WebSocketConnectionState.Closed;
            }
            else
            {
                State = WebSocketConnectionState.CloseReceived;
            }

            // Process the close frame
            WebSocketCloseResult closeResult;
            if (!WebSocketCloseResult.TryParse(frame.Payload, out closeResult))
            {
                closeResult = WebSocketCloseResult.Empty;
            }

            // Make the payload as consumed
            if (payloadLen > 0)
            {
                _inbound.Advance(payload.End);
            }
            return closeResult;
        }

        private Task SendCoreAsync(WebSocketFrame message, WebSocketCloseResult? closeResult, CancellationToken cancellationToken)
        {
            // Base header size is 2 bytes.
            var allocSize = 2;
            var payloadLength = closeResult == null ? message.Payload.Length : closeResult.Value.GetSize();
            if (payloadLength > ushort.MaxValue)
            {
                // We're going to need an 8-byte length
                allocSize += 8;
            }
            else if (payloadLength > 125)
            {
                // We're going to need a 2-byte length
                allocSize += 2;
            }
            if (_maskingKey != null)
            {
                // We need space for the masking key
                allocSize += 4;
            }
            if (closeResult != null)
            {
                // We need space for the close result payload too
                allocSize += payloadLength;
            }

            // Allocate a buffer
            var buffer = _outbound.Alloc(minimumSize: allocSize);
            if (buffer.Memory.Length < allocSize)
            {
                throw new InvalidOperationException("Couldn't allocate enough data from the channel to write the header");
            }

            // Write the opcode and FIN flag
            var opcodeByte = (byte)((int)message.Opcode << 4);
            if (message.EndOfMessage)
            {
                opcodeByte |= 1;
            }
            buffer.WriteBigEndian(opcodeByte);

            // Write the length and mask flag
            var maskingByte = _maskingKey != null ? 0x01 : 0x00; // TODO: Masking flag goes here

            if (payloadLength > ushort.MaxValue)
            {
                buffer.WriteBigEndian((byte)(0xFE | maskingByte));

                // 8-byte length
                buffer.WriteBigEndian((ulong)payloadLength);
            }
            else if (payloadLength > 125)
            {
                buffer.WriteBigEndian((byte)(0xFC | maskingByte));

                // 2-byte length
                buffer.WriteBigEndian((ushort)payloadLength);
            }
            else
            {
                // 1-byte length
                buffer.WriteBigEndian((byte)((payloadLength << 1) | maskingByte));
            }

            var maskingKey = Span<byte>.Empty;
            if (_maskingKey != null)
            {
                // Get a span of the output buffer for the masking key, write it there, then advance the write head.
                maskingKey = buffer.Memory.Slice(0, 4).Span;
                WriteMaskingKey(maskingKey);
                buffer.Advance(4);
            }

            if (closeResult != null)
            {
                // Write the close payload out
                var payload = buffer.Memory.Slice(0, payloadLength).Span;
                closeResult.Value.WriteTo(ref buffer);

                if (_maskingKey != null)
                {
                    MaskingUtilities.ApplyMask(payload, maskingKey);
                }
            }
            else
            {
                // This will copy the actual buffer struct, but NOT the underlying data
                // We need a field so we can by-ref it.
                var payload = message.Payload;

                if (_maskingKey != null)
                {
                    // Mask the payload in it's own buffer
                    MaskingUtilities.ApplyMask(ref payload, maskingKey);
                }

                // Append the (masked) buffer to the output channel
                buffer.Append(payload);
            }


            // Commit and Flush
            return buffer.FlushAsync();
        }
    }
}
