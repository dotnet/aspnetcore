// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebSockets.Protocol
{
    // https://tools.ietf.org/html/rfc6455
    public class CommonWebSocket : WebSocket
    {
        private readonly static byte[] PingBuffer = Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz");
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        private readonly Stream _stream;
        private readonly string _subProtocl;
        private readonly bool _maskOutput;
        private readonly bool _unmaskInput;
        private readonly bool _useZeroMask;
        private readonly SemaphoreSlim _writeLock;
        private readonly Timer _keepAliveTimer;

        private WebSocketState _state;

        private WebSocketCloseStatus? _closeStatus;
        private string _closeStatusDescription;

        private bool _isOutgoingMessageInProgress;

        private byte[] _receiveBuffer;
        private int _receiveBufferOffset;
        private int _receiveBufferBytes;

        private FrameHeader _frameInProgress;
        private long _frameBytesRemaining;
        private int? _firstDataOpCode;
        private int _dataUnmaskOffset;
        private Utilities.Utf8MessageState _incomingUtf8MessageState = new Utilities.Utf8MessageState();

        public CommonWebSocket(Stream stream, string subProtocol, TimeSpan keepAliveInterval, int receiveBufferSize, bool maskOutput, bool useZeroMask, bool unmaskInput)
        {
            _stream = stream;
            _subProtocl = subProtocol;
            _state = WebSocketState.Open;
            _receiveBuffer = new byte[receiveBufferSize];
            _maskOutput = maskOutput;
            _useZeroMask = useZeroMask;
            _unmaskInput = unmaskInput;
            _writeLock = new SemaphoreSlim(1);
            if (keepAliveInterval != Timeout.InfiniteTimeSpan)
            {
                _keepAliveTimer = new Timer(SendKeepAlive, this, keepAliveInterval, keepAliveInterval);
            }
        }

        public static CommonWebSocket CreateClientWebSocket(Stream stream, string subProtocol, TimeSpan keepAliveInterval, int receiveBufferSize, bool useZeroMask)
        {
            return new CommonWebSocket(stream, subProtocol, keepAliveInterval, receiveBufferSize, maskOutput: true, useZeroMask: useZeroMask, unmaskInput: false);
        }

        public static CommonWebSocket CreateServerWebSocket(Stream stream, string subProtocol, TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            return new CommonWebSocket(stream, subProtocol, keepAliveInterval, receiveBufferSize, maskOutput: false, useZeroMask: false, unmaskInput: true);
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get { return _closeStatus; }
        }

        public override string CloseStatusDescription
        {
            get { return _closeStatusDescription; }
        }

        public override WebSocketState State
        {
            get { return _state; }
        }

        public override string SubProtocol
        {
            get { return _subProtocl; }
        }

        // https://tools.ietf.org/html/rfc6455#section-5.3
        // The masking key is a 32-bit value chosen at random by the client.
        // When preparing a masked frame, the client MUST pick a fresh masking
        // key from the set of allowed 32-bit values.  The masking key needs to
        // be unpredictable; thus, the masking key MUST be derived from a strong
        // source of entropy, and the masking key for a given frame MUST NOT
        // make it simple for a server/proxy to predict the masking key for a
        // subsequent frame.  The unpredictability of the masking key is
        // essential to prevent authors of malicious applications from selecting
        // the bytes that appear on the wire.  RFC 4086 [RFC4086] discusses what
        // entails a suitable source of entropy for security-sensitive
        // applications.
        private int GetNextMask()
        {
            if (_useZeroMask)
            {
                return 0;
            }

            // Get 32-bits of randomness and convert it to an int
            var buffer = new byte[sizeof(int)];
            _rng.GetBytes(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            ValidateSegment(buffer);
            if (messageType != WebSocketMessageType.Binary && messageType != WebSocketMessageType.Text)
            {
                // Block control frames
                throw new ArgumentOutOfRangeException(nameof(messageType), messageType, string.Empty);
            }

            // Check concurrent writes, pings & pongs, or closes
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                ThrowIfDisposed();
                ThrowIfOutputClosed();

                int mask = GetNextMask();
                int opcode = _isOutgoingMessageInProgress ? Constants.OpCodes.ContinuationFrame : Utilities.GetOpCode(messageType);
                FrameHeader frameHeader = new FrameHeader(endOfMessage, opcode, _maskOutput, mask, buffer.Count);
                ArraySegment<byte> headerSegment = frameHeader.Buffer;

                if (_maskOutput && mask != 0)
                {
                    // TODO: For larger messages consider using a limited size buffer and masking & sending in segments.
                    byte[] maskedFrame = Utilities.MergeAndMask(mask, headerSegment, buffer);
                    await _stream.WriteAsync(maskedFrame, 0, maskedFrame.Length, cancellationToken);
                }
                else
                {
                    await _stream.WriteAsync(headerSegment.Array, headerSegment.Offset, headerSegment.Count, cancellationToken);
                    await _stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken);
                }

                _isOutgoingMessageInProgress = !endOfMessage;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private static void SendKeepAlive(object state)
        {
            CommonWebSocket websocket = (CommonWebSocket)state;
            websocket.SendKeepAliveAsync();
        }

        private async void SendKeepAliveAsync()
        {
            // Check concurrent writes, pings & pongs, or closes
            if (!_writeLock.Wait(0))
            {
                // Sending real data is better than a ping, discard it.
                return;
            }
            try
            {
                if (State == WebSocketState.CloseSent || State >= WebSocketState.Closed)
                {
                    _keepAliveTimer.Dispose();
                    return;
                }

                int mask = GetNextMask();
                FrameHeader frameHeader = new FrameHeader(true, Constants.OpCodes.PingFrame, _maskOutput, mask, PingBuffer.Length);
                ArraySegment<byte> headerSegment = frameHeader.Buffer;

                // TODO: CancelationToken / timeout?
                if (_maskOutput && mask != 0)
                {
                    byte[] maskedFrame = Utilities.MergeAndMask(mask, headerSegment, new ArraySegment<byte>(PingBuffer));
                    await _stream.WriteAsync(maskedFrame, 0, maskedFrame.Length);
                }
                else
                {
                    await _stream.WriteAsync(headerSegment.Array, headerSegment.Offset, headerSegment.Count);
                    await _stream.WriteAsync(PingBuffer, 0, PingBuffer.Length);
                }
            }
            catch (Exception)
            {
                // TODO: Log exception, this is a background thread.

                // Shut down, we must be in a faulted state;
                Abort();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfInputClosed();
            ValidateSegment(buffer);
            // TODO: InvalidOperationException if any receives are currently in progress.

            // No active frame. Loop because we may be discarding ping/pong frames.
            while (_frameInProgress == null)
            {
                await ReadNextFrameAsync(cancellationToken);
            }

            int opCode = _frameInProgress.OpCode;

            if (opCode == Constants.OpCodes.CloseFrame)
            {
                return await ProcessCloseFrameAsync(cancellationToken);
            }

            // Handle fragmentation, remember the first frame type
            if (opCode == Constants.OpCodes.ContinuationFrame)
            {
                if (!_firstDataOpCode.HasValue)
                {
                    await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Invalid continuation frame", cancellationToken);
                }
                opCode = _firstDataOpCode.Value;
            }
            else
            {
                _firstDataOpCode = opCode;
            }

            // Make sure there's at least some data in the buffer
            int bytesToBuffer = (int)Math.Min((long)_receiveBuffer.Length, _frameBytesRemaining);
            await EnsureDataAvailableOrReadAsync(bytesToBuffer, cancellationToken);

            // Copy buffered data to the users buffer
            int bytesToRead = (int)Math.Min((long)buffer.Count, _frameBytesRemaining);
            int bytesToCopy = Math.Min(bytesToRead, _receiveBufferBytes);
            Array.Copy(_receiveBuffer, _receiveBufferOffset, buffer.Array, buffer.Offset, bytesToCopy);

            if (_unmaskInput)
            {
                // _frameInProgress.Masked == _unmaskInput already verified
                Utilities.MaskInPlace(_frameInProgress.MaskKey, ref _dataUnmaskOffset, new ArraySegment<byte>(buffer.Array, buffer.Offset, bytesToCopy));
            }

            WebSocketReceiveResult result;
            WebSocketMessageType messageType = Utilities.GetMessageType(opCode);

            if (messageType == WebSocketMessageType.Text
                && !Utilities.TryValidateUtf8(new ArraySegment<byte>(buffer.Array, buffer.Offset, bytesToCopy), _frameInProgress.Fin, _incomingUtf8MessageState))
            {
                await SendErrorAbortAndThrow(WebSocketCloseStatus.InvalidPayloadData, "Invalid UTF-8", cancellationToken);
            }

            if (bytesToCopy == _frameBytesRemaining)
            {
                result = new WebSocketReceiveResult(bytesToCopy, messageType, _frameInProgress.Fin);
                if (_frameInProgress.Fin)
                {
                    _firstDataOpCode = null;
                }
                _frameInProgress = null;
                _dataUnmaskOffset = 0;
            }
            else
            {
                result = new WebSocketReceiveResult(bytesToCopy, messageType, false);
            }

            _frameBytesRemaining -= bytesToCopy;
            _receiveBufferBytes -= bytesToCopy;
            _receiveBufferOffset += bytesToCopy;

            return result;
        }

        private async Task ReadNextFrameAsync(CancellationToken cancellationToken)
        {
            await EnsureDataAvailableOrReadAsync(2, cancellationToken);
            int frameHeaderSize = FrameHeader.CalculateFrameHeaderSize(_receiveBuffer[_receiveBufferOffset + 1]);
            await EnsureDataAvailableOrReadAsync(frameHeaderSize, cancellationToken);
            _frameInProgress = new FrameHeader(new ArraySegment<byte>(_receiveBuffer, _receiveBufferOffset, frameHeaderSize));
            _receiveBufferOffset += frameHeaderSize;
            _receiveBufferBytes -= frameHeaderSize;
            _frameBytesRemaining = _frameInProgress.DataLength;

            if (_frameInProgress.AreReservedSet())
            {
                await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Unexpected reserved bits set", cancellationToken);
            }

            if (_unmaskInput != _frameInProgress.Masked)
            {
                await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Incorrect masking", cancellationToken);
            }

            if (!ValidateOpCode(_frameInProgress.OpCode))
            {
                await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Invalid opcode: " + _frameInProgress.OpCode, cancellationToken);
            }

            if (_frameInProgress.IsControlFrame)
            {
                if (_frameBytesRemaining > 125)
                {
                    await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Invalid control frame size", cancellationToken);
                }

                if (!_frameInProgress.Fin)
                {
                    await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Fragmented control frame", cancellationToken);
                }

                if (_frameInProgress.OpCode == Constants.OpCodes.PingFrame || _frameInProgress.OpCode == Constants.OpCodes.PongFrame)
                {
                    // Drain it, should be less than 125 bytes
                    await EnsureDataAvailableOrReadAsync((int)_frameBytesRemaining, cancellationToken);

                    if (_frameInProgress.OpCode == Constants.OpCodes.PingFrame)
                    {
                        await SendPongReplyAsync(cancellationToken);
                    }

                    _receiveBufferOffset += (int)_frameBytesRemaining;
                    _receiveBufferBytes -= (int)_frameBytesRemaining;
                    _frameBytesRemaining = 0;
                    _frameInProgress = null;
                }
            }
            else if (_firstDataOpCode.HasValue && _frameInProgress.OpCode != Constants.OpCodes.ContinuationFrame)
            {
                // A data frame is already in progress, but this new frame is not a continuation frame.
                await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Expected a continuation frame: " + _frameInProgress.OpCode, cancellationToken);
            }
        }

        private async Task EnsureDataAvailableOrReadAsync(int bytesNeeded, CancellationToken cancellationToken)
        {
            // Adequate buffer space?
            Contract.Assert(bytesNeeded <= _receiveBuffer.Length);

            // Insufficient buffered data
            while (_receiveBufferBytes < bytesNeeded)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int spaceRemaining = _receiveBuffer.Length - (_receiveBufferOffset + _receiveBufferBytes);
                if (_receiveBufferOffset > 0 && bytesNeeded > spaceRemaining)
                {
                    // Some data in the buffer, shift down to make room
                    Array.Copy(_receiveBuffer, _receiveBufferOffset, _receiveBuffer, 0, _receiveBufferBytes);
                    _receiveBufferOffset = 0;
                    spaceRemaining = _receiveBuffer.Length - _receiveBufferBytes;
                }
                // Add to the end
                int read = await _stream.ReadAsync(_receiveBuffer, _receiveBufferOffset + _receiveBufferBytes, spaceRemaining, cancellationToken);
                if (read == 0)
                {
                    throw new IOException("Unexpected end of stream");
                }
                _receiveBufferBytes += read;
            }
        }

        // We received a ping, send a pong in reply
        private async Task SendPongReplyAsync(CancellationToken cancellationToken)
        {
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                if (State != WebSocketState.Open)
                {
                    // Output closed, discard the pong.
                    return;
                }

                ArraySegment<byte> dataSegment = new ArraySegment<byte>(_receiveBuffer, _receiveBufferOffset, (int)_frameBytesRemaining);
                if (_unmaskInput)
                {
                    // _frameInProgress.Masked == _unmaskInput already verified
                    Utilities.MaskInPlace(_frameInProgress.MaskKey, dataSegment);
                }

                int mask = GetNextMask();
                FrameHeader header = new FrameHeader(true, Constants.OpCodes.PongFrame, _maskOutput, mask, _frameBytesRemaining);
                if (_maskOutput)
                {
                    Utilities.MaskInPlace(mask, dataSegment);
                }

                ArraySegment<byte> headerSegment = header.Buffer;
                await _stream.WriteAsync(headerSegment.Array, headerSegment.Offset, headerSegment.Count, cancellationToken);
                await _stream.WriteAsync(dataSegment.Array, dataSegment.Offset, dataSegment.Count, cancellationToken);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task<WebSocketReceiveResult> ProcessCloseFrameAsync(CancellationToken cancellationToken)
        {
            // The close message should be less than 125 bytes and fit in the buffer.
            await EnsureDataAvailableOrReadAsync((int)_frameBytesRemaining, CancellationToken.None);

            // Status code and message are optional
            if (_frameBytesRemaining >= 2)
            {
                if (_unmaskInput)
                {
                    Utilities.MaskInPlace(_frameInProgress.MaskKey, new ArraySegment<byte>(_receiveBuffer, _receiveBufferOffset, (int)_frameBytesRemaining));
                }
                _closeStatus = (WebSocketCloseStatus)((_receiveBuffer[_receiveBufferOffset] << 8) | _receiveBuffer[_receiveBufferOffset + 1]);
                if (!ValidateCloseStatus(_closeStatus.Value))
                {
                    await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Invalid close status code.", cancellationToken);
                }
                try
                {
                    var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                    _closeStatusDescription = encoding.GetString(_receiveBuffer, _receiveBufferOffset + 2, (int)_frameBytesRemaining - 2) ?? string.Empty;
                }
                catch (DecoderFallbackException)
                {
                    await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Invalid UTF-8 close message.", cancellationToken);
                }
            }
            else if (_frameBytesRemaining == 1)
            {
                await SendErrorAbortAndThrow(WebSocketCloseStatus.ProtocolError, "Invalid close body.", cancellationToken);
            }
            else
            {
                _closeStatus = _closeStatus ?? WebSocketCloseStatus.NormalClosure;
                _closeStatusDescription = _closeStatusDescription ?? string.Empty;
            }

            Contract.Assert(_frameInProgress.Fin);
            WebSocketReceiveResult result = new WebSocketReceiveResult(0, WebSocketMessageType.Close, _frameInProgress.Fin,
                _closeStatus.Value, _closeStatusDescription);

            if (State == WebSocketState.Open)
            {
                _state = WebSocketState.CloseReceived;
            }
            else if (State == WebSocketState.CloseSent)
            {
                _state = WebSocketState.Closed;
                _stream.Dispose();
            }

            return result;
        }

        public async override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (State == WebSocketState.Open || State == WebSocketState.CloseReceived)
            {
                // Send a close message.
                await CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
            }

            if (State == WebSocketState.CloseSent)
            {
                // Do a receiving drain
                byte[] data = new byte[_receiveBuffer.Length];
                WebSocketReceiveResult result;
                do
                {
                    result = await ReceiveAsync(new ArraySegment<byte>(data), cancellationToken);
                }
                while (result.MessageType != WebSocketMessageType.Close);
            }
        }

        public override async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                ThrowIfDisposed();
                ThrowIfOutputClosed();
                if (_keepAliveTimer != null)
                {
                    _keepAliveTimer.Dispose();
                }

                byte[] descriptionBytes = Encoding.UTF8.GetBytes(statusDescription ?? string.Empty);
                byte[] fullData = new byte[descriptionBytes.Length + 2];
                fullData[0] = (byte)((int)closeStatus >> 8);
                fullData[1] = (byte)closeStatus;
                Array.Copy(descriptionBytes, 0, fullData, 2, descriptionBytes.Length);

                int mask = GetNextMask();
                if (_maskOutput)
                {
                    Utilities.MaskInPlace(mask, new ArraySegment<byte>(fullData));
                }

                FrameHeader frameHeader = new FrameHeader(true, Constants.OpCodes.CloseFrame, _maskOutput, mask, fullData.Length);

                ArraySegment<byte> segment = frameHeader.Buffer;
                await _stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
                await _stream.WriteAsync(fullData, 0, fullData.Length, cancellationToken);

                if (State == WebSocketState.Open)
                {
                    _state = WebSocketState.CloseSent;
                }
                else if (State == WebSocketState.CloseReceived)
                {
                    _state = WebSocketState.Closed;
                    _stream.Dispose();
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public override void Abort()
        {
            if (_state >= WebSocketState.Closed) // or Aborted
            {
                return;
            }

            _state = WebSocketState.Aborted;
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Dispose();
            }
            _stream.Dispose();
        }

        public override void Dispose()
        {
            if (_state >= WebSocketState.Closed) // or Aborted
            {
                return;
            }

            _state = WebSocketState.Closed;
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Dispose();
            }
            _stream.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_state >= WebSocketState.Closed) // or Aborted
            {
                throw new ObjectDisposedException(typeof(CommonWebSocket).FullName);
            }
        }

        private void ThrowIfOutputClosed()
        {
            if (State == WebSocketState.CloseSent)
            {
                throw new InvalidOperationException("Close already sent.");
            }
        }

        private void ThrowIfInputClosed()
        {
            if (State == WebSocketState.CloseReceived)
            {
                throw new InvalidOperationException("Close already received.");
            }
        }

        private void ValidateSegment(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (buffer.Offset < 0 || buffer.Offset > buffer.Array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Offset), buffer.Offset, string.Empty);
            }
            if (buffer.Count < 0 || buffer.Count > buffer.Array.Length - buffer.Offset)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Count), buffer.Count, string.Empty);
            }
        }

        private bool ValidateOpCode(int opCode)
        {
            return Constants.OpCodes.ValidOpCodes.Contains(opCode);
        }

        private static bool ValidateCloseStatus(WebSocketCloseStatus closeStatus)
        {
            if (closeStatus < (WebSocketCloseStatus)1000 || closeStatus >= (WebSocketCloseStatus)5000)
            {
                return false;
            }
            else if (closeStatus >= (WebSocketCloseStatus)3000)
            {
                // 3000-3999 - Reserved for frameworks
                // 4000-4999 - Reserved for private usage
                return true;
            }
            int[] validCodes = new[] { 1000, 1001, 1002, 1003, 1007, 1008, 1009, 1010, 1011 };
            foreach (var validCode in validCodes)
            {
                if (closeStatus == (WebSocketCloseStatus)validCode)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task SendErrorAbortAndThrow(WebSocketCloseStatus error, string message, CancellationToken cancellationToken)
        {
            if (State == WebSocketState.Open || State == WebSocketState.CloseReceived)
            {
                await CloseOutputAsync(error, message, cancellationToken);
            }
            Abort();
            throw new InvalidOperationException(message); // TODO: WebSocketException
        }
    }
}
