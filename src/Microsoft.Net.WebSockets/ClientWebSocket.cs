using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.WebSockets
{
    public class ClientWebSocket : WebSocket
    {
        private readonly Stream _stream;
        private readonly string _subProtocl;
        private WebSocketState _state;

        private WebSocketCloseStatus? _closeStatus;
        private string _closeStatusDescription;

        private byte[] _receiveBuffer;
        private int _receiveOffset;
        private int _receiveCount;

        private FrameHeader _frameInProgress;
        private long _frameBytesRemaining = 0;

        public ClientWebSocket(Stream stream, string subProtocol, int receiveBufferSize)
        {
            _stream = stream;
            _subProtocl = subProtocol;
            _state = WebSocketState.Open;
            _receiveBuffer = new byte[receiveBufferSize];
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

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            // TODO: Validate arguments
            // TODO: Check state
            // TODO: Check concurrent writes
            // TODO: Check ping/pong state
            // TODO: Masking
            FrameHeader frameHeader = new FrameHeader(endOfMessage, GetOpCode(messageType), true, 0, buffer.Count);
            ArraySegment<byte> segment = frameHeader.Buffer;
            await _stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
            await _stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken);
        }

        private int GetOpCode(WebSocketMessageType messageType)
        {
            switch (messageType)
            {
                case WebSocketMessageType.Text: return Constants.OpCodes.TextFrame;
                case WebSocketMessageType.Binary: return Constants.OpCodes.BinaryFrame;
                case WebSocketMessageType.Close: return Constants.OpCodes.CloseFrame;
                default: throw new NotImplementedException(messageType.ToString());
            }
        }

        public async override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            // TODO: Validate arguments
            // TODO: Check state
            // TODO: Check concurrent reads
            // TODO: Check ping/pong state

            // No active frame
            if (_frameInProgress == null)
            {
                await EnsureDataAvailableOrReadAsync(2, cancellationToken);
                int frameHeaderSize = FrameHeader.CalculateFrameHeaderSize(_receiveBuffer[_receiveOffset + 1]);
                await EnsureDataAvailableOrReadAsync(frameHeaderSize, cancellationToken);
                _frameInProgress = new FrameHeader(new ArraySegment<byte>(_receiveBuffer, _receiveOffset, frameHeaderSize));
                _receiveOffset += frameHeaderSize;
                _receiveCount -= frameHeaderSize;
                _frameBytesRemaining = _frameInProgress.DataLength;
            }

            WebSocketReceiveResult result;

            // TODO: Ping or Pong frames

            if (_frameInProgress.OpCode == Constants.OpCodes.CloseFrame)
            {
                // TOOD: This assumes the close message fits in the buffer.
                // TODO: Assert at least two bytes remaining for the close status code.
                await EnsureDataAvailableOrReadAsync((int)_frameBytesRemaining, CancellationToken.None);

                _closeStatus = (WebSocketCloseStatus)((_receiveBuffer[_receiveOffset] << 8) | _receiveBuffer[_receiveOffset + 1]);
                _closeStatusDescription = Encoding.UTF8.GetString(_receiveBuffer, _receiveOffset + 2, _receiveCount - 2) ?? string.Empty;
                result = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, (WebSocketCloseStatus)_closeStatus, _closeStatusDescription);

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

            // Make sure there's at least some data in the buffer
            if (_frameBytesRemaining > 0)
            {
                await EnsureDataAvailableOrReadAsync(1, cancellationToken);
            }

            // Copy buffered data to the users buffer
            int bytesToRead = (int)Math.Min((long)buffer.Count, _frameBytesRemaining);
            if (_receiveCount > 0)
            {
                int bytesToCopy = Math.Min(bytesToRead, _receiveCount);
                Array.Copy(_receiveBuffer, _receiveOffset, buffer.Array, buffer.Offset, bytesToCopy);
                if (bytesToCopy == _frameBytesRemaining)
                {
                    result = new WebSocketReceiveResult(bytesToCopy, GetMessageType(_frameInProgress.OpCode), _frameInProgress.Fin);
                    _frameInProgress = null;
                }
                else
                {
                    result = new WebSocketReceiveResult(bytesToCopy, GetMessageType(_frameInProgress.OpCode), false);
                }
                _frameBytesRemaining -= bytesToCopy;
                _receiveCount -= bytesToCopy;
                _receiveOffset += bytesToCopy;
            }
            else
            {
                // End of an empty frame?
                result = new WebSocketReceiveResult(0, GetMessageType(_frameInProgress.OpCode), true);
            }

            return result;
        }

        private async Task EnsureDataAvailableOrReadAsync(int bytes, CancellationToken cancellationToken)
        {
            // Insufficient data
            while (_receiveCount < bytes && bytes <= _receiveBuffer.Length)
            {
                // Some data in the buffer, shift down to make room
                if (_receiveCount > 0 && _receiveOffset > 0)
                {
                    Array.Copy(_receiveBuffer, _receiveOffset, _receiveBuffer, 0, _receiveCount);
                }
                _receiveOffset = 0;
                // Add to the end
                int read = await _stream.ReadAsync(_receiveBuffer, _receiveCount, _receiveBuffer.Length - (_receiveCount), cancellationToken);
                if (read == 0)
                {
                    throw new IOException("Unexpected end of stream");
                }
                _receiveCount += read;
            }
        }

        private WebSocketMessageType GetMessageType(int opCode)
        {
            switch (opCode)
            {
                case Constants.OpCodes.TextFrame: return WebSocketMessageType.Text;
                case Constants.OpCodes.BinaryFrame: return WebSocketMessageType.Binary;
                case Constants.OpCodes.CloseFrame: return WebSocketMessageType.Close;
                default: throw new NotImplementedException(opCode.ToString());
            }
        }

        public async override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            // TODO: Validate arguments
            // TODO: Check state
            // TODO: Check concurrent writes
            // TODO: Check ping/pong state

            if (State >= WebSocketState.Closed)
            {
                throw new InvalidOperationException("Already closed.");
            }

            if (State == WebSocketState.Open || State == WebSocketState.CloseReceived)
            {
                // Send a close message.
                await CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
            }

            if (State == WebSocketState.CloseSent)
            {
                // Do a receiving drain
                byte[] data = new byte[1024];
                WebSocketReceiveResult result;
                do
                {
                    result = await ReceiveAsync(new ArraySegment<byte>(data), cancellationToken);
                }
                while (result.MessageType != WebSocketMessageType.Close);

                _closeStatus = result.CloseStatus;
                _closeStatusDescription = result.CloseStatusDescription;
            }

            _state = WebSocketState.Closed;
            _stream.Dispose();
        }

        public override async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            // TODO: Validate arguments
            // TODO: Check state
            // TODO: Check concurrent writes
            // TODO: Check ping/pong state

            if (State == WebSocketState.CloseSent || State >= WebSocketState.Closed)
            {
                throw new InvalidOperationException("Already closed.");
            }

            if (State == WebSocketState.Open)
            {
                _state = WebSocketState.CloseSent;
            }
            else if (State == WebSocketState.CloseReceived)
            {
                _state = WebSocketState.Closed;
            }

            byte[] descriptionBytes = Encoding.UTF8.GetBytes(statusDescription ?? string.Empty);
            byte[] fullData = new byte[descriptionBytes.Length + 2];
            fullData[0] = (byte)((int)closeStatus >> 8);
            fullData[1] = (byte)closeStatus;
            Array.Copy(descriptionBytes, 0, fullData, 2, descriptionBytes.Length);

            // TODO: Masking
            FrameHeader frameHeader = new FrameHeader(true, Constants.OpCodes.CloseFrame, true, 0, fullData.Length);
            ArraySegment<byte> segment = frameHeader.Buffer;
            await _stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
            await _stream.WriteAsync(fullData, 0, fullData.Length, cancellationToken);
        }

        public override void Abort()
        {
            if (_state >= WebSocketState.Closed) // or Aborted
            {
                return;
            }

            _state = WebSocketState.Aborted;
            _stream.Dispose();
        }

        public override void Dispose()
        {
            if (_state >= WebSocketState.Closed) // or Aborted
            {
                return;
            }

            _state = WebSocketState.Closed;
            _stream.Dispose();
        }
    }
}
