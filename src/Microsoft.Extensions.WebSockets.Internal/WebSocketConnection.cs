// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Channels;

namespace Microsoft.Extensions.WebSockets.Internal
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
        private WebSocketOptions _options;
        private readonly byte[] _maskingKeyBuffer;
        private readonly IReadableChannel _inbound;
        private readonly IWritableChannel _outbound;
        private readonly CancellationTokenSource _terminateReceiveCts = new CancellationTokenSource();
        private readonly Timer _pinger;
        private readonly CancellationTokenSource _timerCts = new CancellationTokenSource();
        private Utf8Validator _validator = new Utf8Validator();
        private WebSocketOpcode _currentMessageType = WebSocketOpcode.Continuation;

        // Sends must be serialized between SendAsync, Pinger, and the Close frames sent when invalid messages are received.
        private SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public string SubProtocol { get; }

        public WebSocketConnectionState State { get; private set; } = WebSocketConnectionState.Created;

        /// <summary>
        /// Constructs a new, unmasked, <see cref="WebSocketConnection"/> from an <see cref="IReadableChannel"/> and an <see cref="IWritableChannel"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IReadableChannel"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IWritableChannel"/> to which frame will be written when sending.</param>
        public WebSocketConnection(IReadableChannel inbound, IWritableChannel outbound) : this(inbound, outbound, options: WebSocketOptions.DefaultUnmasked) { }

        /// <summary>
        /// Constructs a new, unmasked, <see cref="WebSocketConnection"/> from an <see cref="IReadableChannel"/> and an <see cref="IWritableChannel"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IReadableChannel"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IWritableChannel"/> to which frame will be written when sending.</param>
        /// <param name="subProtocol">The sub-protocol provided during handshaking</param>
        public WebSocketConnection(IReadableChannel inbound, IWritableChannel outbound, string subProtocol) : this(inbound, outbound, subProtocol, options: WebSocketOptions.DefaultUnmasked) { }

        /// <summary>
        /// Constructs a new, <see cref="WebSocketConnection"/> from an <see cref="IReadableChannel"/> and an <see cref="IWritableChannel"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IReadableChannel"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IWritableChannel"/> to which frame will be written when sending.</param>
        /// <param name="options">A <see cref="WebSocketOptions"/> which provides the configuration options for the socket.</param>
        public WebSocketConnection(IReadableChannel inbound, IWritableChannel outbound, WebSocketOptions options) : this(inbound, outbound, subProtocol: string.Empty, options: options) { }

        /// <summary>
        /// Constructs a new <see cref="WebSocketConnection"/> from an <see cref="IReadableChannel"/> and an <see cref="IWritableChannel"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IReadableChannel"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IWritableChannel"/> to which frame will be written when sending.</param>
        /// <param name="subProtocol">The sub-protocol provided during handshaking</param>
        /// <param name="options">A <see cref="WebSocketOptions"/> which provides the configuration options for the socket.</param>
        public WebSocketConnection(IReadableChannel inbound, IWritableChannel outbound, string subProtocol, WebSocketOptions options)
        {
            _inbound = inbound;
            _outbound = outbound;
            _options = options;
            SubProtocol = subProtocol;

            if (_options.FixedMaskingKey != null)
            {
                // Use the fixed key directly as the buffer.
                _maskingKeyBuffer = _options.FixedMaskingKey;

                // Clear the MaskingKeyGenerator just to ensure that nobody set it.
                _options.MaskingKeyGenerator = null;
            }
            else if (_options.MaskingKeyGenerator != null)
            {
                // Establish a buffer for the random generator to use
                _maskingKeyBuffer = new byte[4];
            }

            if (_options.PingInterval > TimeSpan.Zero)
            {
                var pingIntervalMillis = (int)_options.PingInterval.TotalMilliseconds;
                // Set up the pinger
                _pinger = new Timer(Pinger, this, pingIntervalMillis, pingIntervalMillis);
            }
        }

        private static void Pinger(object state)
        {
            var connection = (WebSocketConnection)state;

            // If we are cancelled, don't send the ping
            // Also, if we can't immediately acquire the send lock, we're already sending something, so we don't need the ping.
            if (!connection._timerCts.Token.IsCancellationRequested && connection._sendLock.Wait(0))
            {
                // We don't need to wait for this task to complete, we're "tail calling" and
                // we are in a Timer thread-pool thread.
#pragma warning disable 4014
                connection.SendCoreLockAcquiredAsync(
                    fin: true,
                    opcode: WebSocketOpcode.Ping,
                    payloadAllocLength: 28,
                    payloadLength: 28,
                    payloadWriter: PingPayloadWriter,
                    payload: DateTime.UtcNow,
                    cancellationToken: connection._timerCts.Token);
#pragma warning restore 4014
            }
        }

        public void Dispose()
        {
            State = WebSocketConnectionState.Closed;
            _pinger?.Dispose();
            _timerCts.Cancel();
            _terminateReceiveCts.Cancel();
            _inbound.Complete();
            _outbound.Complete();
        }

        public Task<WebSocketCloseResult> ExecuteAsync(Func<WebSocketFrame, object, Task> messageHandler, object state)
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
            return ReceiveLoop(messageHandler, state, _terminateReceiveCts.Token);
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
                throw new InvalidOperationException($"Cannot send until the connection is started using {nameof(ExecuteAsync)}");
            }
            else if (State == WebSocketConnectionState.CloseSent)
            {
                throw new InvalidOperationException("Cannot send after sending a Close frame");
            }

            if (frame.Opcode == WebSocketOpcode.Close)
            {
                throw new InvalidOperationException($"Cannot use {nameof(SendAsync)} to send a Close frame, use {nameof(CloseAsync)} instead.");
            }
            return SendCoreAsync(
                fin: frame.EndOfMessage,
                opcode: frame.Opcode,
                payloadAllocLength: 0, // We don't copy the payload, we append it, so we don't need any alloc for the payload
                payloadLength: frame.Payload.Length,
                payloadWriter: AppendPayloadWriter,
                payload: frame.Payload,
                cancellationToken: cancellationToken);
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

            var payloadSize = result.GetSize();
            await SendCoreAsync(
                fin: true,
                opcode: WebSocketOpcode.Close,
                payloadAllocLength: payloadSize,
                payloadLength: payloadSize,
                payloadWriter: CloseResultPayloadWriter,
                payload: result,
                cancellationToken: cancellationToken);

            _timerCts.Cancel();
            _pinger?.Dispose();

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
            if (_options.MaskingKeyGenerator != null)
            {
                // Get a new random mask
                // Until https://github.com/dotnet/corefx/issues/12323 is fixed we need to use this shared buffer and copy model
                // Once we have that fix we should be able to generate the mask directly into the output buffer.
                _options.MaskingKeyGenerator.GetBytes(_maskingKeyBuffer);
            }

            buffer.Set(_maskingKeyBuffer);
        }

        private async Task<WebSocketCloseResult> ReceiveLoop(Func<WebSocketFrame, object, Task> messageHandler, object state, CancellationToken cancellationToken)
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

                var fin = (opcodeByte & 0x80) != 0;
                var opcodeNum = opcodeByte & 0x0F;
                var opcode = (WebSocketOpcode)opcodeNum;

                if ((opcodeByte & 0x70) != 0)
                {
                    // Reserved bits set, this frame is invalid, close our side and terminate immediately
                    return await CloseFromProtocolError(cancellationToken, 0, default(ReadableBuffer), "Reserved bits, which are required to be zero, were set.");
                }
                else if ((opcodeNum >= 0x03 && opcodeNum <= 0x07) || (opcodeNum >= 0x0B && opcodeNum <= 0x0F))
                {
                    // Reserved opcode
                    return await CloseFromProtocolError(cancellationToken, 0, default(ReadableBuffer), $"Received frame using reserved opcode: 0x{opcodeNum:X}");
                }

                // Read the first byte of the payload length
                var lenByte = buffer.ReadBigEndian<byte>();
                buffer = buffer.Slice(1);

                var masked = (lenByte & 0x80) != 0;
                var payloadLen = (lenByte & 0x7F);

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
                    headerLength += 8;
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

                if (frame.Opcode.IsControl() && !frame.EndOfMessage)
                {
                    // Control frames cannot be fragmented.
                    return await CloseFromProtocolError(cancellationToken, payloadLen, payload, "Control frames may not be fragmented");
                }
                else if (_currentMessageType != WebSocketOpcode.Continuation && opcode.IsMessage() && opcode != 0)
                {
                    return await CloseFromProtocolError(cancellationToken, payloadLen, payload, "Received non-continuation frame during a fragmented message");
                }
                else if (_currentMessageType == WebSocketOpcode.Continuation && frame.Opcode == WebSocketOpcode.Continuation)
                {
                    return await CloseFromProtocolError(cancellationToken, payloadLen, payload, "Continuation Frame was received when expecting a new message");
                }

                if (frame.Opcode == WebSocketOpcode.Close)
                {
                    // Allowed frame lengths:
                    //  0 - No body
                    //  2 - Code with no reason phrase
                    //  >2 - Code and reason phrase (must be valid UTF-8)
                    if (frame.Payload.Length > 125)
                    {
                        return await CloseFromProtocolError(cancellationToken, payloadLen, payload, "Close frame payload too long. Maximum size is 125 bytes");
                    }
                    else if ((frame.Payload.Length == 1) || (frame.Payload.Length > 2 && !Utf8Validator.ValidateUtf8(payload.Slice(2))))
                    {
                        return await CloseFromProtocolError(cancellationToken, payloadLen, payload, "Close frame payload invalid");
                    }

                    ushort? actualStatusCode;
                    var closeResult = HandleCloseFrame(payload, frame, out actualStatusCode);

                    // Verify the close result
                    if (actualStatusCode != null)
                    {
                        var statusCode = actualStatusCode.Value;
                        if (statusCode < 1000 || statusCode == 1004 || statusCode == 1005 || statusCode == 1006 || (statusCode > 1011 && statusCode < 3000))
                        {
                            return await CloseFromProtocolError(cancellationToken, payloadLen, payload, $"Invalid close status: {statusCode}.");
                        }
                    }

                    // Make the payload as consumed
                    if (payloadLen > 0)
                    {
                        _inbound.Advance(payload.End);
                    }

                    return closeResult;
                }
                else
                {
                    if (frame.Opcode == WebSocketOpcode.Ping)
                    {
                        // Check the ping payload length
                        if (frame.Payload.Length > 125)
                        {
                            // Payload too long
                            return await CloseFromProtocolError(cancellationToken, payloadLen, payload, "Ping frame exceeded maximum size of 125 bytes");
                        }

                        await SendCoreAsync(
                            frame.EndOfMessage,
                            WebSocketOpcode.Pong,
                            payloadAllocLength: 0,
                            payloadLength: payload.Length,
                            payloadWriter: AppendPayloadWriter,
                            payload: payload,
                            cancellationToken: cancellationToken);
                    }
                    var effectiveOpcode = opcode == WebSocketOpcode.Continuation ? _currentMessageType : opcode;
                    if (effectiveOpcode == WebSocketOpcode.Text && !_validator.ValidateUtf8Frame(frame.Payload, frame.EndOfMessage))
                    {
                        // Drop the frame and immediately close with InvalidPayload
                        return await CloseFromProtocolError(cancellationToken, payloadLen, payload, "An invalid Text frame payload was received", statusCode: WebSocketCloseStatus.InvalidPayloadData);
                    }
                    else if (_options.PassAllFramesThrough || (frame.Opcode != WebSocketOpcode.Ping && frame.Opcode != WebSocketOpcode.Pong))
                    {
                        await messageHandler(frame, state);
                    }
                }

                if (fin)
                {
                    // Reset the UTF8 validator
                    _validator.Reset();

                    // If it's a non-control frame, reset the message type tracker
                    if (opcode.IsMessage())
                    {
                        _currentMessageType = WebSocketOpcode.Continuation;
                    }
                }
                // If there isn't a current message type, and this was a fragmented message frame, set the current message type
                else if (!fin && _currentMessageType == WebSocketOpcode.Continuation && opcode.IsMessage())
                {
                    _currentMessageType = opcode;
                }

                // Mark the payload as consumed
                if (payloadLen > 0)
                {
                    _inbound.Advance(payload.End);
                }
            }
            return WebSocketCloseResult.AbnormalClosure;
        }

        private async Task<WebSocketCloseResult> CloseFromProtocolError(CancellationToken cancellationToken, int payloadLen, ReadableBuffer payload, string reason, WebSocketCloseStatus statusCode = WebSocketCloseStatus.ProtocolError)
        {
            // Non-continuation non-control message during fragmented message
            if (payloadLen > 0)
            {
                _inbound.Advance(payload.End);
            }
            var closeResult = new WebSocketCloseResult(
                statusCode,
                reason);
            await CloseAsync(closeResult, cancellationToken);
            Dispose();
            return closeResult;
        }

        private WebSocketCloseResult HandleCloseFrame(ReadableBuffer payload, WebSocketFrame frame, out ushort? actualStatusCode)
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
            if (!WebSocketCloseResult.TryParse(frame.Payload, out closeResult, out actualStatusCode))
            {
                closeResult = WebSocketCloseResult.Empty;
            }
            return closeResult;
        }

        private static void PingPayloadWriter(WritableBuffer output, Span<byte> maskingKey, int payloadLength, DateTime timestamp)
        {
            var payload = output.Memory.Slice(0, payloadLength);

            // TODO: Don't put this string on the heap? Is there a way to do that without re-implementing ToString?
            // Ideally we'd like to render the string directly to the output buffer.
            var str = timestamp.ToString("O", CultureInfo.InvariantCulture);

            ArraySegment<byte> buffer;
            if (payload.TryGetArray(out buffer))
            {
                // Fast path - Write the encoded bytes directly out.
                Encoding.UTF8.GetBytes(str, 0, str.Length, buffer.Array, buffer.Offset);
            }
            else
            {
                // TODO: Could use TryGetPointer, GetBytes does take a byte*, but it seems like just waiting until we have a version that uses Span is best.
                // Slow path - Allocate a heap buffer for the encoded bytes before writing them out.
                payload.Span.Set(Encoding.UTF8.GetBytes(str));
            }

            if (maskingKey.Length > 0)
            {
                MaskingUtilities.ApplyMask(payload.Span, maskingKey);
            }

            output.Advance(payloadLength);
        }

        private static void CloseResultPayloadWriter(WritableBuffer output, Span<byte> maskingKey, int payloadLength, WebSocketCloseResult result)
        {
            // Write the close payload out
            var payload = output.Memory.Slice(0, payloadLength).Span;
            result.WriteTo(ref output);

            if (maskingKey.Length > 0)
            {
                MaskingUtilities.ApplyMask(payload, maskingKey);
            }
        }

        private static void AppendPayloadWriter(WritableBuffer output, Span<byte> maskingKey, int payloadLength, ReadableBuffer payload)
        {
            if (maskingKey.Length > 0)
            {
                // Mask the payload in it's own buffer
                MaskingUtilities.ApplyMask(ref payload, maskingKey);
            }

            output.Append(payload);
        }

        private Task SendCoreAsync<T>(bool fin, WebSocketOpcode opcode, int payloadAllocLength, int payloadLength, Action<WritableBuffer, Span<byte>, int, T> payloadWriter, T payload, CancellationToken cancellationToken)
        {
            if (_sendLock.Wait(0))
            {
                return SendCoreLockAcquiredAsync(fin, opcode, payloadAllocLength, payloadLength, payloadWriter, payload, cancellationToken);
            }
            else
            {
                return SendCoreWaitForLockAsync(fin, opcode, payloadAllocLength, payloadLength, payloadWriter, payload, cancellationToken);
            }
        }

        private async Task SendCoreWaitForLockAsync<T>(bool fin, WebSocketOpcode opcode, int payloadAllocLength, int payloadLength, Action<WritableBuffer, Span<byte>, int, T> payloadWriter, T payload, CancellationToken cancellationToken)
        {
            await _sendLock.WaitAsync(cancellationToken);
            await SendCoreLockAcquiredAsync(fin, opcode, payloadAllocLength, payloadLength, payloadWriter, payload, cancellationToken);
        }

        private async Task SendCoreLockAcquiredAsync<T>(bool fin, WebSocketOpcode opcode, int payloadAllocLength, int payloadLength, Action<WritableBuffer, Span<byte>, int, T> payloadWriter, T payload, CancellationToken cancellationToken)
        {
            try
            {
                // Ensure the lock is held
                Debug.Assert(_sendLock.CurrentCount == 0);

                // Base header size is 2 bytes.
                WritableBuffer buffer;
                var allocSize = CalculateAllocSize(payloadAllocLength, payloadLength);

                // Allocate a buffer
                buffer = _outbound.Alloc(minimumSize: allocSize);
                Debug.Assert(buffer.Memory.Length >= allocSize);

                // Write the opcode and FIN flag
                var opcodeByte = (byte)opcode;
                if (fin)
                {
                    opcodeByte |= 0x80;
                }
                buffer.WriteBigEndian(opcodeByte);

                // Write the length and mask flag
                WritePayloadLength(payloadLength, buffer);

                var maskingKey = Span<byte>.Empty;
                if (_maskingKeyBuffer != null)
                {
                    // Get a span of the output buffer for the masking key, write it there, then advance the write head.
                    maskingKey = buffer.Memory.Slice(0, 4).Span;
                    WriteMaskingKey(maskingKey);
                    buffer.Advance(4);
                }

                // Write the payload
                payloadWriter(buffer, maskingKey, payloadLength, payload);

                // Flush.
                await buffer.FlushAsync();
            }
            finally
            {
                // Unlock.
                _sendLock.Release();
            }
        }

        private int CalculateAllocSize(int payloadAllocLength, int payloadLength)
        {
            var allocSize = 2;
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
            if (_maskingKeyBuffer != null)
            {
                // We need space for the masking key
                allocSize += 4;
            }

            // We may need space for the payload too
            return allocSize + payloadAllocLength;
        }

        private void WritePayloadLength(int payloadLength, WritableBuffer buffer)
        {
            var maskingByte = _maskingKeyBuffer != null ? 0x80 : 0x00;

            if (payloadLength > ushort.MaxValue)
            {
                buffer.WriteBigEndian((byte)(0x7F | maskingByte));

                // 8-byte length
                buffer.WriteBigEndian((ulong)payloadLength);
            }
            else if (payloadLength > 125)
            {
                buffer.WriteBigEndian((byte)(0x7E | maskingByte));

                // 2-byte length
                buffer.WriteBigEndian((ushort)payloadLength);
            }
            else
            {
                // 1-byte length
                buffer.WriteBigEndian((byte)(payloadLength | maskingByte));
            }
        }
    }
}
