// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class WebSocketsTransport : ITransport
    {
        private readonly ClientWebSocket _webSocket = new ClientWebSocket();
        private IChannelConnection<SendMessage, Message> _application;
        private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();
        private readonly ILogger _logger;

        public WebSocketsTransport()
            : this(null)
        {
        }

        public WebSocketsTransport(ILoggerFactory loggerFactory)
        {
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(nameof(WebSocketsTransport));
        }

        public Task Running { get; private set; } = Task.CompletedTask;

        public async Task StartAsync(Uri url, IChannelConnection<SendMessage, Message> application)
        {
            _logger.LogInformation("Starting {0}", nameof(WebSocketsTransport));

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
            var sendTask = SendMessages(url);
            var receiveTask = ReceiveMessages(url);

            // TODO: Handle TCP connection errors
            // https://github.com/SignalR/SignalR/blob/1fba14fa3437e24c204dfaf8a18db3fce8acad3c/src/Microsoft.AspNet.SignalR.Core/Owin/WebSockets/WebSocketHandler.cs#L248-L251
            Running = Task.WhenAll(sendTask, receiveTask).ContinueWith(t =>
            {
                _logger.LogDebug("Transport stopped. Exception: '{0}'", t.Exception?.InnerException);

                _application.Output.TryComplete(t.IsFaulted ? t.Exception.InnerException : null);
                return t;
            }).Unwrap();
        }

        private async Task ReceiveMessages(Uri pollUrl)
        {
            _logger.LogInformation("Starting receive loop");

            try
            {
                while (!_transportCts.Token.IsCancellationRequested)
                {
                    const int bufferSize = 4096;
                    var totalBytes = 0;
                    var incomingMessage = new List<ArraySegment<byte>>();
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        var buffer = new ArraySegment<byte>(new byte[bufferSize]);

                        //Exceptions are handled above where the send and receive tasks are being run.
                        receiveResult = await _webSocket.ReceiveAsync(buffer, _transportCts.Token);
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogInformation("Websocket closed by the server. Close status {0}", receiveResult.CloseStatus);

                            _application.Output.Complete(
                                receiveResult.CloseStatus == WebSocketCloseStatus.NormalClosure
                                ? null
                                : new InvalidOperationException(
                                    $"Websocket closed with error: {receiveResult.CloseStatus}."));
                            return;
                        }

                        _logger.LogDebug("Message received. Type: {0}, size: {1}, EndOfMessage: {2}",
                            receiveResult.MessageType.ToString(), receiveResult.Count, receiveResult.EndOfMessage);

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

                    _logger.LogInformation("Passing message to application. Payload size: {0}", message.Payload.Length);
                    while (await _application.Output.WaitToWriteAsync(_transportCts.Token))
                    {
                        if (_application.Output.TryWrite(message))
                        {
                            incomingMessage.Clear();
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _transportCts.Cancel();
                _logger.LogInformation("Receive loop stopped");
            }
        }

        private async Task SendMessages(Uri sendUrl)
        {
            _logger.LogInformation("Starting the send loop");

            try
            {
                while (await _application.Input.WaitToReadAsync(_transportCts.Token))
                {
                    while (_application.Input.TryRead(out SendMessage message))
                    {
                        try
                        {
                            _logger.LogDebug("Received message from application. Message type {0}. Payload size: {1}",
                                message.Type, message.Payload.Length);

                            await _webSocket.SendAsync(new ArraySegment<byte>(message.Payload),
                                message.Type == MessageType.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                                true, _transportCts.Token);

                            message.SendResult.SetResult(null);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Sending a message canceled.");
                            message.SendResult.SetCanceled();
                            await CloseWebSocket();
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error while sending a message {0}", ex.Message);
                            message.SendResult.SetException(ex);
                            await CloseWebSocket();
                            throw;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _transportCts.Cancel();
                _logger.LogInformation("Send loop stopped");
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

            await _webSocket.ConnectAsync(uriBuilder.Uri, CancellationToken.None);
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Transport {0} is stopping", nameof(WebSocketsTransport));

            await CloseWebSocket();
            _webSocket.Dispose();

            try
            {
                await Running;
            }
            catch
            {
                // exceptions have been handled in the Running task continuation by closing the channel with the exception
            }

            _logger.LogInformation("Transport {0} stopped", nameof(WebSocketsTransport));
        }

        private async Task CloseWebSocket()
        {
            try
            {
                // Best effort - it's still possible (but not likely) that the transport is being closed via StopAsync
                // while the webSocket is being closed due to an error.
                if (_webSocket.State != WebSocketState.Closed)
                {
                    _logger.LogInformation("Closing webSocket");
                    await _webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                // This is benign - the exception can happen due to the race described above because we would
                // try closing the webSocket twice.
                _logger.LogInformation("Closing webSocket failed with {0}", ex);
            }
        }
    }
}
