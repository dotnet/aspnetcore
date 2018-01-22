// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class WebSocketsTransport : ITransport
    {
        private readonly ClientWebSocket _webSocket;
        private Channel<byte[], SendMessage> _application;
        private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _receiveCts = new CancellationTokenSource();
        private readonly ILogger _logger;

        public Task Running { get; private set; } = Task.CompletedTask;

        public TransferMode? Mode { get; private set; }

        public WebSocketsTransport()
            : this(null, null)
        {
        }

        public WebSocketsTransport(HttpOptions httpOptions, ILoggerFactory loggerFactory)
        {
            _webSocket = new ClientWebSocket();

            if (httpOptions?.Headers != null)
            {
                foreach (var header in httpOptions.Headers)
                {
                    _webSocket.Options.SetRequestHeader(header.Key, header.Value);
                }
            }

            if (httpOptions?.JwtBearerTokenFactory != null)
            {
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {httpOptions.JwtBearerTokenFactory()}");
            }

            httpOptions?.WebSocketOptions?.Invoke(_webSocket.Options);

            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<WebSocketsTransport>();
        }

        public async Task StartAsync(Uri url, Channel<byte[], SendMessage> application, TransferMode requestedTransferMode, IConnection connection)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (requestedTransferMode != TransferMode.Binary && requestedTransferMode != TransferMode.Text)
            {
                throw new ArgumentException("Invalid transfer mode.", nameof(requestedTransferMode));
            }

            _application = application;
            Mode = requestedTransferMode;

            _logger.StartTransport(Mode.Value);

            await Connect(url);
            var sendTask = SendMessages(url);
            var receiveTask = ReceiveMessages(url);

            // TODO: Handle TCP connection errors
            // https://github.com/SignalR/SignalR/blob/1fba14fa3437e24c204dfaf8a18db3fce8acad3c/src/Microsoft.AspNet.SignalR.Core/Owin/WebSockets/WebSocketHandler.cs#L248-L251
            Running = Task.WhenAll(sendTask, receiveTask).ContinueWith(t =>
            {
                _webSocket.Dispose();
                _logger.TransportStopped(t.Exception?.InnerException);
                _application.Writer.TryComplete(t.IsFaulted ? t.Exception.InnerException : null);
                return t;
            }).Unwrap();
        }

        private async Task ReceiveMessages(Uri pollUrl)
        {
            _logger.StartReceive();

            try
            {
                while (!_receiveCts.Token.IsCancellationRequested)
                {
                    const int bufferSize = 4096;
                    var totalBytes = 0;
                    var incomingMessage = new List<ArraySegment<byte>>();
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        var buffer = new ArraySegment<byte>(new byte[bufferSize]);

                        //Exceptions are handled above where the send and receive tasks are being run.
                        receiveResult = await _webSocket.ReceiveAsync(buffer, _receiveCts.Token);
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.WebSocketClosed(receiveResult.CloseStatus);

                            _application.Writer.Complete(
                                receiveResult.CloseStatus == WebSocketCloseStatus.NormalClosure
                                ? null
                                : new InvalidOperationException(
                                    $"Websocket closed with error: {receiveResult.CloseStatus}."));
                            return;
                        }

                        _logger.MessageReceived(receiveResult.MessageType, receiveResult.Count, receiveResult.EndOfMessage);

                        var truncBuffer = new ArraySegment<byte>(buffer.Array, 0, receiveResult.Count);
                        incomingMessage.Add(truncBuffer);
                        totalBytes += receiveResult.Count;
                    } while (!receiveResult.EndOfMessage);

                    //Making sure the message type is either text or binary
                    Debug.Assert((receiveResult.MessageType == WebSocketMessageType.Binary || receiveResult.MessageType == WebSocketMessageType.Text), "Unexpected message type");

                    var messageBuffer = new byte[totalBytes];
                    if (incomingMessage.Count > 1)
                    {
                        var offset = 0;
                        for (var i = 0; i < incomingMessage.Count; i++)
                        {
                            Buffer.BlockCopy(incomingMessage[i].Array, 0, messageBuffer, offset, incomingMessage[i].Count);
                            offset += incomingMessage[i].Count;
                        }
                    }
                    else
                    {
                        Buffer.BlockCopy(incomingMessage[0].Array, incomingMessage[0].Offset, messageBuffer, 0, incomingMessage[0].Count);
                    }

                    try
                    {
                        if (!_transportCts.Token.IsCancellationRequested)
                        {
                            _logger.MessageToApp(messageBuffer.Length);
                            while (await _application.Writer.WaitToWriteAsync(_transportCts.Token))
                            {
                                if (_application.Writer.TryWrite(messageBuffer))
                                {
                                    incomingMessage.Clear();
                                    break;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.CancelMessage();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.ReceiveCanceled();
            }
            finally
            {
                _logger.ReceiveStopped();
                _transportCts.Cancel();
            }
        }

        private async Task SendMessages(Uri sendUrl)
        {
            _logger.SendStarted();

            var webSocketMessageType =
                Mode == TransferMode.Binary
                    ? WebSocketMessageType.Binary
                    : WebSocketMessageType.Text;

            try
            {
                while (await _application.Reader.WaitToReadAsync(_transportCts.Token))
                {
                    while (_application.Reader.TryRead(out SendMessage message))
                    {
                        try
                        {
                            _logger.ReceivedFromApp(message.Payload.Length);

                            await _webSocket.SendAsync(new ArraySegment<byte>(message.Payload), webSocketMessageType, true, _transportCts.Token);

                            message.SendResult.SetResult(null);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.SendMessageCanceled();
                            message.SendResult.SetCanceled();
                            await CloseWebSocket();
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorSendingMessage(ex);
                            message.SendResult.SetException(ex);
                            await CloseWebSocket();
                            throw;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.SendCanceled();
            }
            finally
            {
                _logger.SendStopped();
                TriggerCancel();
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
            _logger.TransportStopping();

            await CloseWebSocket();

            try
            {
                await Running;
            }
            catch
            {
                // exceptions have been handled in the Running task continuation by closing the channel with the exception
            }
        }

        private async Task CloseWebSocket()
        {
            try
            {
                // Best effort - it's still possible (but not likely) that the transport is being closed via StopAsync
                // while the webSocket is being closed due to an error.
                if (_webSocket.State != WebSocketState.Closed)
                {
                    _logger.ClosingWebSocket();

                    // We intentionally don't pass _transportCts.Token to CloseOutputAsync. The token can be cancelled
                    // for reasons not related to webSocket in which case we would not close the websocket gracefully.
                    await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);

                    // shutdown the transport after a timeout in case the server does not send close frame
                    TriggerCancel();
                }
            }
            catch (Exception ex)
            {
                // This is benign - the exception can happen due to the race described above because we would
                // try closing the webSocket twice.
                _logger.ClosingWebSocketFailed(ex);
            }
        }

        private void TriggerCancel()
        {
            // Give server 5 seconds to respond with a close frame for graceful close.
            _receiveCts.CancelAfter(TimeSpan.FromSeconds(5));
            _transportCts.Cancel();
        }
    }
}
