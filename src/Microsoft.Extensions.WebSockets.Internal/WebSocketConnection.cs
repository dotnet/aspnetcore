// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.WebSockets.Internal
{
    /// <summary>
    /// Provides the default implementation of <see cref="IWebSocketConnection"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is thread-safe, as long as only one thread ever calls <see cref="ExecuteAsync"/>. Multiple threads may call <see cref="SendAsync"/> simultaneously
    /// and the sends will block until ongoing send operations complete.
    /// </para>
    /// <para>
    /// The general pattern of having a single thread running <see cref="ExecuteAsync"/> and a separate thread running <see cref="SendAsync"/> will
    /// be thread-safe, as each method interacts with completely separate state.
    /// </para>
    /// </remarks>
    public class WebSocketConnection : IWebSocketConnection
    {
        private WebSocketOptions _options;
        private readonly byte[] _maskingKeyBuffer;
        private readonly IPipeReader _inbound;
        private readonly IPipeWriter _outbound;
        private readonly Timer _pinger;
        private readonly CancellationTokenSource _timerCts = new CancellationTokenSource();
        private Utf8Validator _validator = new Utf8Validator();
        private WebSocketOpcode _currentMessageType = WebSocketOpcode.Continuation;

        // Sends must be serialized between SendAsync, Pinger, and the Close frames sent when invalid messages are received.
        private SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public string SubProtocol { get; }

        public WebSocketConnectionState State { get; private set; } = WebSocketConnectionState.Created;

        /// <summary>
        /// Constructs a new, unmasked, <see cref="WebSocketConnection"/> from an <see cref="IPipeReader"/> and an <see cref="IPipeWriter"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IPipeReader"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IPipeWriter"/> to which frame will be written when sending.</param>
        public WebSocketConnection(IPipeReader inbound, IPipeWriter outbound) : this(inbound, outbound, options: WebSocketOptions.DefaultUnmasked) { }

        /// <summary>
        /// Constructs a new, unmasked, <see cref="WebSocketConnection"/> from an <see cref="IPipeReader"/> and an <see cref="IPipeWriter"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IPipeReader"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IPipeWriter"/> to which frame will be written when sending.</param>
        /// <param name="subProtocol">The sub-protocol provided during handshaking</param>
        public WebSocketConnection(IPipeReader inbound, IPipeWriter outbound, string subProtocol) : this(inbound, outbound, subProtocol, options: WebSocketOptions.DefaultUnmasked) { }

        /// <summary>
        /// Constructs a new, <see cref="WebSocketConnection"/> from an <see cref="IPipeReader"/> and an <see cref="IPipeWriter"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IPipeReader"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IPipeWriter"/> to which frame will be written when sending.</param>
        /// <param name="options">A <see cref="WebSocketOptions"/> which provides the configuration options for the socket.</param>
        public WebSocketConnection(IPipeReader inbound, IPipeWriter outbound, WebSocketOptions options) : this(inbound, outbound, subProtocol: string.Empty, options: options) { }

        /// <summary>
        /// Constructs a new <see cref="WebSocketConnection"/> from an <see cref="IPipeReader"/> and an <see cref="IPipeWriter"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="inbound">A <see cref="IPipeReader"/> from which frames will be read when receiving.</param>
        /// <param name="outbound">A <see cref="IPipeWriter"/> to which frame will be written when sending.</param>
        /// <param name="subProtocol">The sub-protocol provided during handshaking</param>
        /// <param name="options">A <see cref="WebSocketOptions"/> which provides the configuration options for the socket.</param>
        public WebSocketConnection(IPipeReader inbound, IPipeWriter outbound, string subProtocol, WebSocketOptions options)
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
                var ignore = connection.SendCoreLockAcquiredAsync(
                    fin: true,
                    opcode: WebSocketOpcode.Ping,
                    payloadAllocLength: 28,
                    payloadLength: 28,
                    payloadWriter: PingPayloadWriter,
                    payload: DateTime.UtcNow,
                    cancellationToken: connection._timerCts.Token);
            }
        }

        public void Dispose()
        {
            State = WebSocketConnectionState.Closed;
            _pinger?.Dispose();
            _timerCts.Cancel();
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
            return ReceiveLoop(messageHandler, state);
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
                // Already closed
                return;
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

            _maskingKeyBuffer.CopyTo(buffer);
        }

        /// <summary>
        /// Terminates the socket abruptly.
        /// </summary>
        public void Abort()
        {
            // We duplicate some work from Dispose here, but that's OK.
            _timerCts.Cancel();
            _inbound.CancelPendingRead();
            _outbound.Complete();
        }

        private async ValueTask<(bool Success, byte OpcodeByte, bool Masked, bool Fin, int Length, uint MaskingKey)> ReadHeaderAsync()
        {
            // Read at least 2 bytes
            var readResult = await _inbound.ReadAtLeastAsync(2);
            if (readResult.IsCancelled || (readResult.IsCompleted && readResult.Buffer.Length < 2))
            {
                _inbound.Advance(readResult.Buffer.End);
                return (Success: false, OpcodeByte: 0, Masked: false, Fin: false, Length: 0, MaskingKey: 0);
            }
            var buffer = readResult.Buffer;

            // Read the opcode and length
            var opcodeByte = buffer.ReadBigEndian<byte>();
            buffer = buffer.Slice(1);

            // Read the first byte of the payload length
            var lengthByte = buffer.ReadBigEndian<byte>();
            buffer = buffer.Slice(1);

            _inbound.Advance(buffer.Start);

            // Determine how much header there still is to read
            var fin = (opcodeByte & 0x80) != 0;
            var masked = (lengthByte & 0x80) != 0;
            var length = lengthByte & 0x7F;

            // Calculate the rest of the header length
            var headerLength = masked ? 4 : 0;
            if (length == 126)
            {
                headerLength += 2;
            }
            else if (length == 127)
            {
                headerLength += 8;
            }

            // Read the next set of header data
            uint maskingKey = 0;
            if (headerLength > 0)
            {
                readResult = await _inbound.ReadAtLeastAsync(headerLength);
                if (readResult.IsCancelled || (readResult.IsCompleted && readResult.Buffer.Length < headerLength))
                {
                    _inbound.Advance(readResult.Buffer.End);
                    return (Success: false, OpcodeByte: 0, Masked: false, Fin: false, Length: 0, MaskingKey: 0);
                }
                buffer = readResult.Buffer;

                // Read extended payload length (if any)
                if (length == 126)
                {
                    length = buffer.ReadBigEndian<ushort>();
                    buffer = buffer.Slice(sizeof(ushort));
                }
                else if (length == 127)
                {
                    var longLen = buffer.ReadBigEndian<ulong>();
                    buffer = buffer.Slice(sizeof(ulong));
                    if (longLen > int.MaxValue)
                    {
                        throw new WebSocketException($"Frame is too large. Maximum frame size is {int.MaxValue} bytes");
                    }
                    length = (int)longLen;
                }

                // Read masking key
                if (masked)
                {
                    var maskingKeyStart = buffer.Start;
                    maskingKey = buffer.Slice(0, sizeof(uint)).ReadBigEndian<uint>();
                    buffer = buffer.Slice(sizeof(uint));
                }

                // Mark the length and masking key consumed
                _inbound.Advance(buffer.Start);
            }

            return (Success: true, opcodeByte, masked, fin, length, maskingKey);
        }

        private async ValueTask<(bool Success, ReadableBuffer Buffer)> ReadPayloadAsync(int length, bool masked, uint maskingKey)
        {
            var payload = default(ReadableBuffer);
            if (length > 0)
            {
                var readResult = await _inbound.ReadAtLeastAsync(length);
                if (readResult.IsCancelled || (readResult.IsCompleted && readResult.Buffer.Length < length))
                {
                    return (Success: false, Buffer: readResult.Buffer);
                }
                var buffer = readResult.Buffer;

                payload = buffer.Slice(0, length);

                if (masked)
                {
                    // Unmask
                    MaskingUtilities.ApplyMask(ref payload, maskingKey);
                }
            }
            return (Success: true, Buffer: payload);
        }

        private async Task<WebSocketCloseResult> ReceiveLoop(Func<WebSocketFrame, object, Task> messageHandler, object state)
        {
            try
            {
                while (true)
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

                    var header = await ReadHeaderAsync();
                    if (!header.Success)
                    {
                        break;
                    }

                    // Validate Opcode
                    var opcodeNum = header.OpcodeByte & 0x0F;

                    if ((header.OpcodeByte & 0x70) != 0)
                    {
                        // Reserved bits set, this frame is invalid, close our side and terminate immediately
                        await CloseFromProtocolError("Reserved bits, which are required to be zero, were set.");
                        break;
                    }
                    else if ((opcodeNum >= 0x03 && opcodeNum <= 0x07) || (opcodeNum >= 0x0B && opcodeNum <= 0x0F))
                    {
                        // Reserved opcode
                        await CloseFromProtocolError($"Received frame using reserved opcode: 0x{opcodeNum:X}");
                        break;
                    }
                    var opcode = (WebSocketOpcode)opcodeNum;

                    var payload = await ReadPayloadAsync(header.Length, header.Masked, header.MaskingKey);
                    if (!payload.Success)
                    {
                        _inbound.Advance(payload.Buffer.End);
                        break;
                    }

                    var frame = new WebSocketFrame(header.Fin, opcode, payload.Buffer);

                    // Start a try-finally because we may get an exception while closing, if there's an error
                    // And we need to advance the buffer even if that happens. It wasn't needed above because
                    // we had already parsed the buffer before we verified it, so we had already advanced the
                    // buffer, if we encountered an error while closing we didn't have to advance the buffer.
                    // Side Note: Look at this gloriously aligned comment. You have anurse and brecon to thank for it. Oh wait, I ruined it.
                    try
                    {
                        if (frame.Opcode.IsControl() && !frame.EndOfMessage)
                        {
                            // Control frames cannot be fragmented.
                            await CloseFromProtocolError("Control frames may not be fragmented");
                            break;
                        }
                        else if (_currentMessageType != WebSocketOpcode.Continuation && opcode.IsMessage() && opcode != 0)
                        {
                            await CloseFromProtocolError("Received non-continuation frame during a fragmented message");
                            break;
                        }
                        else if (_currentMessageType == WebSocketOpcode.Continuation && frame.Opcode == WebSocketOpcode.Continuation)
                        {
                            await CloseFromProtocolError("Continuation Frame was received when expecting a new message");
                            break;
                        }

                        if (frame.Opcode == WebSocketOpcode.Close)
                        {
                            return await ProcessCloseFrameAsync(frame);
                        }
                        else
                        {
                            if (frame.Opcode == WebSocketOpcode.Ping)
                            {
                                // Check the ping payload length
                                if (frame.Payload.Length > 125)
                                {
                                    // Payload too long
                                    await CloseFromProtocolError("Ping frame exceeded maximum size of 125 bytes");
                                    break;
                                }

                                await SendCoreAsync(
                                    frame.EndOfMessage,
                                    WebSocketOpcode.Pong,
                                    payloadAllocLength: 0,
                                    payloadLength: frame.Payload.Length,
                                    payloadWriter: AppendPayloadWriter,
                                    payload: frame.Payload,
                                    cancellationToken: CancellationToken.None);
                            }
                            var effectiveOpcode = opcode == WebSocketOpcode.Continuation ? _currentMessageType : opcode;
                            if (effectiveOpcode == WebSocketOpcode.Text && !_validator.ValidateUtf8Frame(frame.Payload, frame.EndOfMessage))
                            {
                                // Drop the frame and immediately close with InvalidPayload
                                await CloseFromProtocolError("An invalid Text frame payload was received", statusCode: WebSocketCloseStatus.InvalidPayloadData);
                                break;
                            }
                            else if (_options.PassAllFramesThrough || (frame.Opcode != WebSocketOpcode.Ping && frame.Opcode != WebSocketOpcode.Pong))
                            {
                                await messageHandler(frame, state);
                            }
                        }
                    }
                    finally
                    {
                        if (frame.Payload.Length > 0)
                        {
                            _inbound.Advance(frame.Payload.End);
                        }
                    }

                    if (header.Fin)
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
                    else if (!header.Fin && _currentMessageType == WebSocketOpcode.Continuation && opcode.IsMessage())
                    {
                        _currentMessageType = opcode;
                    }
                }
            }
            catch
            {
                // Abort the socket and rethrow
                Abort();
                throw;
            }
            return WebSocketCloseResult.AbnormalClosure;
        }

        private async ValueTask<WebSocketCloseResult> ProcessCloseFrameAsync(WebSocketFrame frame)
        {
            // Allowed frame lengths:
            //  0 - No body
            //  2 - Code with no reason phrase
            //  >2 - Code and reason phrase (must be valid UTF-8)
            if (frame.Payload.Length > 125)
            {
                await CloseFromProtocolError("Close frame payload too long. Maximum size is 125 bytes");
                return WebSocketCloseResult.AbnormalClosure;
            }
            else if ((frame.Payload.Length == 1) || (frame.Payload.Length > 2 && !Utf8Validator.ValidateUtf8(frame.Payload.Slice(2))))
            {
                await CloseFromProtocolError("Close frame payload invalid");
                return WebSocketCloseResult.AbnormalClosure;
            }

            ushort? actualStatusCode;
            var closeResult = ParseCloseFrame(frame.Payload, frame, out actualStatusCode);

            // Verify the close result
            if (actualStatusCode != null)
            {
                var statusCode = actualStatusCode.Value;
                if (statusCode < 1000 || statusCode == 1004 || statusCode == 1005 || statusCode == 1006 || (statusCode > 1011 && statusCode < 3000))
                {
                    await CloseFromProtocolError($"Invalid close status: {statusCode}.");
                    return WebSocketCloseResult.AbnormalClosure;
                }
            }

            return closeResult;
        }

        private async Task CloseFromProtocolError(string reason, WebSocketCloseStatus statusCode = WebSocketCloseStatus.ProtocolError)
        {
            var closeResult = new WebSocketCloseResult(
                statusCode,
                reason);
            await CloseAsync(closeResult, CancellationToken.None);

            // We can now terminate our connection, according to the spec.
            Abort();
        }

        private WebSocketCloseResult ParseCloseFrame(ReadableBuffer payload, WebSocketFrame frame, out ushort? actualStatusCode)
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
            var payload = output.Buffer.Slice(0, payloadLength);

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
                Encoding.UTF8.GetBytes(str).CopyTo(payload.Span);
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
            var payload = output.Buffer.Slice(0, payloadLength).Span;
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
                Debug.Assert(buffer.Buffer.Length >= allocSize);

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
                    maskingKey = buffer.Buffer.Slice(0, 4).Span;
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
