// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class WebSocketsTransport : ITransport
    {
        private ClientWebSocket _webSocket = new ClientWebSocket();
        private IChannelConnection<Message> _application;
        private CancellationToken _cancellationToken = new CancellationToken();
        private readonly ILogger _logger;

        public WebSocketsTransport()
            : this(null)
        {
        }

        public WebSocketsTransport(ILoggerFactory loggerFactory)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger("WebSocketsTransport");
        }

        public Task Running { get; private set; }

        public async Task StartAsync(Uri url, IChannelConnection<Message> application)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            _application = application;

            await Connect(url);
            var sendTask = SendMessages(url, _cancellationToken);
            var receiveTask = ReceiveMessages(url, _cancellationToken);

            // TODO: Handle TCP connection errors
            // https://github.com/SignalR/SignalR/blob/1fba14fa3437e24c204dfaf8a18db3fce8acad3c/src/Microsoft.AspNet.SignalR.Core/Owin/WebSockets/WebSocketHandler.cs#L248-L251
            Running = Task.WhenAll(sendTask, receiveTask).ContinueWith(t =>
            {
                _application.Output.TryComplete(t.IsFaulted ? t.Exception.InnerException : null);
                return t;
            }).Unwrap();
        }

        private async Task ReceiveMessages(Uri pollUrl, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                const int bufferSize = 1024;
                var totalBytes = 0;
                var incomingMessage = new List<ArraySegment<byte>>();
                WebSocketReceiveResult receiveResult;
                do
                {
                    var buffer = new ArraySegment<byte>(new byte[bufferSize]);

                    //Exceptions are handled above where the send and receive tasks are being run.
                    receiveResult = await _webSocket.ReceiveAsync(buffer, cancellationToken);
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        _application.Output.Complete();
                        return;
                    }
                    var truncBuffer = new ArraySegment<byte>(buffer.Array, 0, receiveResult.Count);
                    incomingMessage.Add(truncBuffer);
                    totalBytes += receiveResult.Count;
                } while (!receiveResult.EndOfMessage);

                //Making sure the message type is either text or binary
                Debug.Assert((receiveResult.MessageType == WebSocketMessageType.Binary || receiveResult.MessageType == WebSocketMessageType.Text), "Unexpected message type");

                Message message;
                var messageType = receiveResult.MessageType == WebSocketMessageType.Binary ? MessageType.Binary : MessageType.Text;
                if (incomingMessage.Count > 1)
                {
                    var messageBuffer = new byte[totalBytes];
                    var offset = 0;
                    for (var i = 0; i < incomingMessage.Count; i++)
                    {
                        Buffer.BlockCopy(incomingMessage[i].Array, 0, messageBuffer, offset, incomingMessage[i].Count);
                        offset += incomingMessage[i].Count;
                    }

                    message = new Message(messageBuffer, messageType, receiveResult.EndOfMessage);
                }
                else
                {
                    var buffer = new byte[incomingMessage[0].Count];
                    Buffer.BlockCopy(incomingMessage[0].Array, incomingMessage[0].Offset, buffer, 0, incomingMessage[0].Count);
                    message = new Message(buffer, messageType, receiveResult.EndOfMessage);
                }

                while (await _application.Output.WaitToWriteAsync(cancellationToken))
                {
                    if (_application.Output.TryWrite(message))
                    {
                        incomingMessage.Clear();
                        break;
                    }
                }
            }
        }

        private async Task SendMessages(Uri sendUrl, CancellationToken cancellationToken)
        {
            while (await _application.Input.WaitToReadAsync(cancellationToken))
            {
                Message message;
                while (_application.Input.TryRead(out message))
                {
                    try
                    {
                        await _webSocket.SendAsync(new ArraySegment<byte>(message.Payload),
                        message.Type == MessageType.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary, true,
                        cancellationToken);
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger?.LogError(ex.Message);
                        await _webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, _cancellationToken);
                        break;
                    }
                }
            }
        }

        private async Task Connect(Uri url)
        {
            var uriBuilder = new UriBuilder(url);
            if (url.Scheme == "http")
            {
                uriBuilder.Scheme = "ws";
            }
            else if (url.Scheme == "https")
            {
                uriBuilder.Scheme = "wss";
            }

            await _webSocket.ConnectAsync(uriBuilder.Uri, _cancellationToken);
        }

        public async Task StopAsync()
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, _cancellationToken);
            _webSocket.Dispose();

            try
            {
                await Running;
            }
            catch
            {
                // exceptions have been handled in the Running task continuation by closing the channel with the exception
            }
        }
    }
}
