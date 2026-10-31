// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Tests;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

internal class TestWebSocketConnectionFeature : IHttpWebSocketFeature, IDisposable
{
    public TestWebSocketConnectionFeature()
    { }
    public TestWebSocketConnectionFeature(SyncPoint sync)
    {
        _sync = sync;
    }

    private readonly SyncPoint _sync;
    private readonly TaskCompletionSource _accepted = new TaskCompletionSource();

    public bool IsWebSocketRequest => true;

    public WebSocketChannel Client { get; private set; }

    public string SubProtocol { get; private set; }

    public Task Accepted => _accepted.Task;

    public Task<WebSocket> AcceptAsync() => AcceptAsync(new WebSocketAcceptContext());

    public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
    {
        var clientToServer = Channel.CreateUnbounded<WebSocketMessage>();
        var serverToClient = Channel.CreateUnbounded<WebSocketMessage>();

        var clientSocket = new WebSocketChannel(serverToClient.Reader, clientToServer.Writer, _sync);
        var serverSocket = new WebSocketChannel(clientToServer.Reader, serverToClient.Writer, _sync);

        Client = clientSocket;
        SubProtocol = context.SubProtocol;

        _accepted.TrySetResult();
        return Task.FromResult<WebSocket>(serverSocket);
    }

    public void Dispose()
    {
    }

    public class WebSocketChannel : WebSocket
    {
        private readonly ChannelReader<WebSocketMessage> _input;
        private readonly ChannelWriter<WebSocketMessage> _output;
        private readonly SyncPoint _sync;

        private WebSocketCloseStatus? _closeStatus;
        private string _closeStatusDescription;
        private WebSocketState _state;
        private WebSocketMessage _internalBuffer = new WebSocketMessage();

        public WebSocketChannel(ChannelReader<WebSocketMessage> input, ChannelWriter<WebSocketMessage> output, SyncPoint sync = null)
        {
            _input = input;
            _output = output;
            _sync = sync;
        }

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;

        public override string CloseStatusDescription => _closeStatusDescription;

        public override WebSocketState State => _state;

        public override string SubProtocol => null;

        public override void Abort()
        {
            _output.TryComplete(new OperationCanceledException());
            _state = WebSocketState.Aborted;
        }

        public void SendAbort()
        {
            _output.TryComplete(new WebSocketException(WebSocketError.ConnectionClosedPrematurely));
        }

        public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            await SendMessageAsync(new WebSocketMessage
            {
                CloseStatus = closeStatus,
                CloseStatusDescription = statusDescription,
                MessageType = WebSocketMessageType.Close,
            },
            cancellationToken);

            _state = WebSocketState.CloseSent;

            _output.TryComplete();
        }

        public override async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            await SendMessageAsync(new WebSocketMessage
            {
                CloseStatus = closeStatus,
                CloseStatusDescription = statusDescription,
                MessageType = WebSocketMessageType.Close,
            },
            cancellationToken);

            _state = WebSocketState.CloseSent;

            _output.TryComplete();
        }

        public override void Dispose()
        {
            _state = WebSocketState.Closed;
            _output.TryComplete();
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                if (_internalBuffer.Buffer == null || _internalBuffer.Buffer.Length == 0)
                {
                    await _input.WaitToReadAsync(cancellationToken);

                    if (_input.TryRead(out var message))
                    {
                        if (message.MessageType == WebSocketMessageType.Close)
                        {
                            _state = WebSocketState.CloseReceived;
                            _closeStatus = message.CloseStatus;
                            _closeStatusDescription = message.CloseStatusDescription;
                            return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, message.CloseStatus, message.CloseStatusDescription);
                        }

                        _internalBuffer = message;
                    }
                }

                var length = _internalBuffer.Buffer.Length;
                if (buffer.Count - buffer.Offset < _internalBuffer.Buffer.Length)
                {
                    length = Math.Min(buffer.Count - buffer.Offset, _internalBuffer.Buffer.Length);
                    Buffer.BlockCopy(_internalBuffer.Buffer, 0, buffer.Array, buffer.Offset, length);
                }
                else
                {
                    Buffer.BlockCopy(_internalBuffer.Buffer, 0, buffer.Array, buffer.Offset, length);
                }

                var endOfMessage = _internalBuffer.EndOfMessage;
                if (length > 0)
                {
                    // Remove the sent bytes from the remaining buffer
                    _internalBuffer.Buffer = _internalBuffer.Buffer.AsMemory().Slice(length).ToArray();
                    endOfMessage = _internalBuffer.Buffer.Length == 0 && endOfMessage;
                }

                return new WebSocketReceiveResult(length, _internalBuffer.MessageType, endOfMessage);
            }
            catch (WebSocketException ex)
            {
                switch (ex.WebSocketErrorCode)
                {
                    case WebSocketError.ConnectionClosedPrematurely:
                        _state = WebSocketState.Aborted;
                        break;
                }

                // Complete the client side if there's an error
                _output.TryComplete();

                throw;
            }

            throw new InvalidOperationException("Unexpected close");
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (_sync != null)
            {
                await _sync.WaitToContinue();
            }
            cancellationToken.ThrowIfCancellationRequested();

            var copy = new byte[buffer.Count];
            Buffer.BlockCopy(buffer.Array, buffer.Offset, copy, 0, buffer.Count);
            await SendMessageAsync(new WebSocketMessage
            {
                Buffer = copy,
                MessageType = messageType,
                EndOfMessage = endOfMessage
            },
            cancellationToken);
        }

        public async Task<WebSocketMessage> GetNextMessageAsync()
        {
            while (await _input.WaitToReadAsync())
            {
                if (_input.TryRead(out var message))
                {
                    return message;
                }
            }

            return new WebSocketMessage()
            {
                Buffer = Array.Empty<byte>(),
                MessageType = WebSocketMessageType.Close,
                EndOfMessage = true,
                CloseStatus = WebSocketCloseStatus.InternalServerError,
                CloseStatusDescription = string.Empty
            };
        }

        public async Task<WebSocketConnectionSummary> ExecuteAndCaptureFramesAsync()
        {
            var frames = new List<WebSocketMessage>();
            while (await _input.WaitToReadAsync())
            {
                while (_input.TryRead(out var message))
                {
                    if (message.MessageType == WebSocketMessageType.Close)
                    {
                        _state = WebSocketState.CloseReceived;
                        _closeStatus = message.CloseStatus;
                        _closeStatusDescription = message.CloseStatusDescription;
                        return new WebSocketConnectionSummary(frames, new WebSocketReceiveResult(0, message.MessageType, message.EndOfMessage, message.CloseStatus, message.CloseStatusDescription));
                    }

                    frames.Add(message);
                }
            }
            _state = WebSocketState.Closed;
            _closeStatus = WebSocketCloseStatus.InternalServerError;
            return new WebSocketConnectionSummary(frames, new WebSocketReceiveResult(0, WebSocketMessageType.Close, endOfMessage: true, closeStatus: WebSocketCloseStatus.InternalServerError, closeStatusDescription: ""));
        }

        private async Task SendMessageAsync(WebSocketMessage webSocketMessage, CancellationToken cancellationToken)
        {
            while (await _output.WaitToWriteAsync(cancellationToken))
            {
                if (_output.TryWrite(webSocketMessage))
                {
                    break;
                }
            }
        }
    }

    public class WebSocketConnectionSummary
    {
        public IList<WebSocketMessage> Received { get; }
        public WebSocketReceiveResult CloseResult { get; }

        public WebSocketConnectionSummary(IList<WebSocketMessage> received, WebSocketReceiveResult closeResult)
        {
            Received = received;
            CloseResult = closeResult;
        }
    }

    public class WebSocketMessage
    {
        public byte[] Buffer { get; set; }
        public WebSocketMessageType MessageType { get; set; }
        public bool EndOfMessage { get; set; }
        public WebSocketCloseStatus? CloseStatus { get; set; }
        public string CloseStatusDescription { get; set; }
    }
}
