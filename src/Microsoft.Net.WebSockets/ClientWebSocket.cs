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
            get { throw new NotImplementedException(); }
        }

        public override string CloseStatusDescription
        {
            get { throw new NotImplementedException(); }
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
            // TODO: Close frame
            // TODO: Ping or Pong frames

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

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            // TODO: Validate arguments
            // TODO: Check state
            // TODO: Check concurrent writes
            // TODO: Check ping/pong state
            _state = WebSocketState.CloseSent;

            // TODO: Masking
            byte[] buffer = Encoding.UTF8.GetBytes(statusDescription);
            FrameHeader frameHeader = new FrameHeader(true, Constants.OpCodes.CloseFrame, true, 0, buffer.Length);
            ArraySegment<byte> segment = frameHeader.Buffer;
            await _stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
            await _stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
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
