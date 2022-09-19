// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;

namespace Microsoft.AspNetCore.TestHost;

internal sealed class TestWebSocket : WebSocket
{
    private readonly ReceiverSenderBuffer _receiveBuffer;
    private readonly ReceiverSenderBuffer _sendBuffer;
    private readonly string? _subProtocol;
    private WebSocketState _state;
    private WebSocketCloseStatus? _closeStatus;
    private string? _closeStatusDescription;
    private Message? _receiveMessage;

    public static Tuple<TestWebSocket, TestWebSocket> CreatePair(string? subProtocol)
    {
        var buffers = new[] { new ReceiverSenderBuffer(), new ReceiverSenderBuffer() };
        return Tuple.Create(
            new TestWebSocket(subProtocol, buffers[0], buffers[1]),
            new TestWebSocket(subProtocol, buffers[1], buffers[0]));
    }

    public override WebSocketCloseStatus? CloseStatus
    {
        get { return _closeStatus; }
    }

    public override string? CloseStatusDescription
    {
        get { return _closeStatusDescription; }
    }

    public override WebSocketState State
    {
        get { return _state; }
    }

    public override string? SubProtocol
    {
        get { return _subProtocol; }
    }

    public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
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
            var data = new byte[1024];
            WebSocketReceiveResult result;
            do
            {
                result = await ReceiveAsync(new ArraySegment<byte>(data), cancellationToken);
            }
            while (result.MessageType != WebSocketMessageType.Close);
        }
    }

    public override async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ThrowIfOutputClosed();

        var message = new Message(closeStatus, statusDescription);
        await _sendBuffer.SendAsync(message);

        if (State == WebSocketState.Open)
        {
            _state = WebSocketState.CloseSent;
        }
        else if (State == WebSocketState.CloseReceived)
        {
            _state = WebSocketState.Closed;
            Close();
        }
    }

    public override void Abort()
    {
        if (_state >= WebSocketState.Closed) // or Aborted
        {
            return;
        }

        _state = WebSocketState.Aborted;
        Close();
    }

    public override void Dispose()
    {
        if (_state >= WebSocketState.Closed) // or Aborted
        {
            return;
        }

        _state = WebSocketState.Closed;
        Close();
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ThrowIfInputClosed();
        ValidateSegment(buffer);
        // TODO: InvalidOperationException if any receives are currently in progress.

        Message? receiveMessage = _receiveMessage;
        _receiveMessage = null;
        if (receiveMessage == null)
        {
            receiveMessage = await _receiveBuffer.ReceiveAsync(cancellationToken);
        }
        if (receiveMessage.MessageType == WebSocketMessageType.Close)
        {
            _closeStatus = receiveMessage.CloseStatus;
            _closeStatusDescription = receiveMessage.CloseStatusDescription ?? string.Empty;
            var result = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, _closeStatus, _closeStatusDescription);
            if (_state == WebSocketState.Open)
            {
                _state = WebSocketState.CloseReceived;
            }
            else if (_state == WebSocketState.CloseSent)
            {
                _state = WebSocketState.Closed;
                Close();
            }
            return result;
        }
        else
        {
            int count = Math.Min(buffer.Count, receiveMessage.Buffer.Count);
            bool endOfMessage = count == receiveMessage.Buffer.Count;
            Array.Copy(receiveMessage.Buffer.Array!, receiveMessage.Buffer.Offset, buffer.Array!, buffer.Offset, count);
            if (!endOfMessage)
            {
                receiveMessage.Buffer = new ArraySegment<byte>(receiveMessage.Buffer.Array!, receiveMessage.Buffer.Offset + count, receiveMessage.Buffer.Count - count);
                _receiveMessage = receiveMessage;
            }
            endOfMessage = endOfMessage && receiveMessage.EndOfMessage;
            return new WebSocketReceiveResult(count, receiveMessage.MessageType, endOfMessage);
        }
    }

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        ValidateSegment(buffer);
        if (messageType != WebSocketMessageType.Binary && messageType != WebSocketMessageType.Text)
        {
            // Block control frames
            throw new ArgumentOutOfRangeException(nameof(messageType), messageType, string.Empty);
        }

        var message = new Message(buffer, messageType, endOfMessage);
        return _sendBuffer.SendAsync(message);
    }

    private void Close()
    {
        _receiveBuffer.SetReceiverClosed();
        _sendBuffer.SetSenderClosed();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_state >= WebSocketState.Closed, typeof(TestWebSocket)); // or Aborted
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

    private static void ValidateSegment(ArraySegment<byte> buffer)
    {
        if (buffer.Array == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }
        if (buffer.Offset < 0 || buffer.Offset > buffer.Array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Offset, string.Empty);
        }
        if (buffer.Count < 0 || buffer.Count > buffer.Array.Length - buffer.Offset)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Count, string.Empty);
        }
    }

    private TestWebSocket(string? subProtocol, ReceiverSenderBuffer readBuffer, ReceiverSenderBuffer writeBuffer)
    {
        _state = WebSocketState.Open;
        _subProtocol = subProtocol;
        _receiveBuffer = readBuffer;
        _sendBuffer = writeBuffer;
    }

    private sealed class Message
    {
        public Message(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage)
        {
            Buffer = buffer;
            CloseStatus = null;
            CloseStatusDescription = null;
            EndOfMessage = endOfMessage;
            MessageType = messageType;
        }

        public Message(WebSocketCloseStatus? closeStatus, string? closeStatusDescription)
        {
            Buffer = new ArraySegment<byte>(Array.Empty<byte>());
            CloseStatus = closeStatus;
            CloseStatusDescription = closeStatusDescription;
            MessageType = WebSocketMessageType.Close;
            EndOfMessage = true;
        }

        public WebSocketCloseStatus? CloseStatus { get; set; }
        public string? CloseStatusDescription { get; set; }
        public ArraySegment<byte> Buffer { get; set; }
        public bool EndOfMessage { get; set; }
        public WebSocketMessageType MessageType { get; set; }
    }

    private sealed class ReceiverSenderBuffer
    {
        private bool _receiverClosed;
        private bool _senderClosed;
        private bool _disposed;
        private readonly SemaphoreSlim _sem;
        private readonly Queue<Message> _messageQueue;

        public ReceiverSenderBuffer()
        {
            _sem = new SemaphoreSlim(0);
            _messageQueue = new Queue<Message>();
        }

        public async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                ThrowNoReceive();
            }
            await _sem.WaitAsync(cancellationToken);
            lock (_messageQueue)
            {
                if (_messageQueue.Count == 0)
                {
                    _disposed = true;
                    _sem.Dispose();
                    ThrowNoReceive();
                }
                return _messageQueue.Dequeue();
            }
        }

        public Task SendAsync(Message message)
        {
            lock (_messageQueue)
            {
                ObjectDisposedException.ThrowIf(_senderClosed, typeof(TestWebSocket));

                if (_receiverClosed)
                {
                    throw new IOException("The remote end closed the connection.", new ObjectDisposedException(typeof(TestWebSocket).FullName));
                }

                // we return immediately so we need to copy the buffer since the sender can re-use it
                var array = new byte[message.Buffer.Count];
                Array.Copy(message.Buffer.Array!, message.Buffer.Offset, array, 0, message.Buffer.Count);
                message.Buffer = new ArraySegment<byte>(array);

                _messageQueue.Enqueue(message);
                _sem.Release();

                return Task.FromResult(true);
            }
        }

        public void SetReceiverClosed()
        {
            lock (_messageQueue)
            {
                if (!_receiverClosed)
                {
                    _receiverClosed = true;
                    if (!_disposed)
                    {
                        _sem.Release();
                    }
                }
            }
        }

        public void SetSenderClosed()
        {
            lock (_messageQueue)
            {
                if (!_senderClosed)
                {
                    _senderClosed = true;
                    if (!_disposed)
                    {
                        _sem.Release();
                    }
                }
            }
        }

        private void ThrowNoReceive()
        {
            ObjectDisposedException.ThrowIf(_receiverClosed, typeof(TestWebSocket));
            
            // _senderClosed must be true.
            throw new IOException("The remote end closed the connection.", new ObjectDisposedException(typeof(TestWebSocket).FullName));
        }
    }
}
