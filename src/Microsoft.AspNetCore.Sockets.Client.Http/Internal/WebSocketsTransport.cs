// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Sockets.Client.Internal
{
    public partial class WebSocketsTransport : ITransport
    {
        private readonly ClientWebSocket _webSocket;
        private IDuplexPipe _application;
        private WebSocketMessageType _webSocketMessageType;
        private readonly ILogger _logger;
        private readonly TimeSpan _closeTimeout;
        private volatile bool _aborted;

        public Task Running { get; private set; } = Task.CompletedTask;

        public WebSocketsTransport()
            : this(null, null)
        {
        }

        public WebSocketsTransport(HttpOptions httpOptions, ILoggerFactory loggerFactory)
        {
            _webSocket = new ClientWebSocket();

            // Issue in ClientWebSocket prevents user-agent being set - https://github.com/dotnet/corefx/issues/26627
            //_webSocket.Options.SetRequestHeader("User-Agent", Constants.UserAgentHeader.ToString());

            if (httpOptions != null)
            {
                if (httpOptions.Headers != null)
                {
                    foreach (var header in httpOptions.Headers)
                    {
                        _webSocket.Options.SetRequestHeader(header.Key, header.Value);
                    }
                }

                if (httpOptions.Cookies != null)
                {
                    _webSocket.Options.Cookies = httpOptions.Cookies;
                }

                if (httpOptions.ClientCertificates != null)
                {
                    _webSocket.Options.ClientCertificates.AddRange(httpOptions.ClientCertificates);
                }

                if (httpOptions.Credentials != null)
                {
                    _webSocket.Options.Credentials = httpOptions.Credentials;
                }

                if (httpOptions.Proxy != null)
                {
                    _webSocket.Options.Proxy = httpOptions.Proxy;
                }

                if (httpOptions.UseDefaultCredentials != null)
                {
                    _webSocket.Options.UseDefaultCredentials = httpOptions.UseDefaultCredentials.Value;
                }

                if (httpOptions.AccessTokenFactory != null)
                {
                    _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {httpOptions.AccessTokenFactory()}");
                }

                httpOptions.WebSocketOptions?.Invoke(_webSocket.Options);

                _closeTimeout = httpOptions.CloseTimeout;
            }

            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<WebSocketsTransport>();
        }

        public async Task StartAsync(Uri url, IDuplexPipe application, TransferFormat transferFormat, IConnection connection)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (transferFormat != TransferFormat.Binary && transferFormat != TransferFormat.Text)
            {
                throw new ArgumentException($"The '{transferFormat}' transfer format is not supported by this transport.", nameof(transferFormat));
            }

            _application = application;
            _webSocketMessageType = transferFormat == TransferFormat.Binary
                ? WebSocketMessageType.Binary
                : WebSocketMessageType.Text;

            var resolvedUrl = ResolveWebSocketsUrl(url);

            Log.StartTransport(_logger, transferFormat, resolvedUrl);

            await _webSocket.ConnectAsync(resolvedUrl, CancellationToken.None);

            // TODO: Handle TCP connection errors
            // https://github.com/SignalR/SignalR/blob/1fba14fa3437e24c204dfaf8a18db3fce8acad3c/src/Microsoft.AspNet.SignalR.Core/Owin/WebSockets/WebSocketHandler.cs#L248-L251
            Running = ProcessSocketAsync(_webSocket);
        }

        private async Task ProcessSocketAsync(WebSocket socket)
        {
            using (socket)
            {
                // Begin sending and receiving.
                var receiving = StartReceiving(socket);
                var sending = StartSending(socket);

                // Wait for send or receive to complete
                var trigger = await Task.WhenAny(receiving, sending);

                if (trigger == receiving)
                {
                    // We're waiting for the application to finish and there are 2 things it could be doing
                    // 1. Waiting for application data
                    // 2. Waiting for a websocket send to complete

                    // Cancel the application so that ReadAsync yields
                    _application.Input.CancelPendingRead();

                    using (var delayCts = new CancellationTokenSource())
                    {
                        var resultTask = await Task.WhenAny(sending, Task.Delay(_closeTimeout, delayCts.Token));

                        if (resultTask != sending)
                        {
                            _aborted = true;

                            // Abort the websocket if we're stuck in a pending send to the client
                            socket.Abort();
                        }
                        else
                        {
                            // Cancel the timeout
                            delayCts.Cancel();
                        }
                    }
                }
                else
                {
                    // We're waiting on the websocket to close and there are 2 things it could be doing
                    // 1. Waiting for websocket data
                    // 2. Waiting on a flush to complete (backpressure being applied)

                    _aborted = true;

                    // Abort the websocket if we're stuck in a pending receive from the client
                    socket.Abort();

                    // Cancel any pending flush so that we can quit
                    _application.Output.CancelPendingFlush();
                }
            }
        }

        private async Task StartReceiving(WebSocket socket)
        {
            try
            {
                while (true)
                {
                    var memory = _application.Output.GetMemory();

#if NETCOREAPP2_1
                    var receiveResult = await socket.ReceiveAsync(memory, CancellationToken.None);
#else
                    var isArray = MemoryMarshal.TryGetArray<byte>(memory, out var arraySegment);
                    Debug.Assert(isArray);

                    // Exceptions are handled above where the send and receive tasks are being run.
                    var receiveResult = await socket.ReceiveAsync(arraySegment, CancellationToken.None);
#endif
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        Log.WebSocketClosed(_logger, _webSocket.CloseStatus);

                        if (_webSocket.CloseStatus != WebSocketCloseStatus.NormalClosure)
                        {
                            throw new InvalidOperationException($"Websocket closed with error: {_webSocket.CloseStatus}.");
                        }

                        return;
                    }

                    Log.MessageReceived(_logger, receiveResult.MessageType, receiveResult.Count, receiveResult.EndOfMessage);

                    _application.Output.Advance(receiveResult.Count);

                    var flushResult = await _application.Output.FlushAsync();

                    // We canceled in the middle of applying back pressure
                    // or if the consumer is done
                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.ReceiveCanceled(_logger);
            }
            catch (Exception ex)
            {
                if (!_aborted)
                {
                    _application.Output.Complete(ex);

                    // We re-throw here so we can communicate that there was an error when sending
                    // the close frame
                    throw;
                }
            }
            finally
            {
                // We're done writing
                _application.Output.Complete();

                Log.ReceiveStopped(_logger);
            }
        }

        private async Task StartSending(WebSocket socket)
        {
            Exception error = null;

            try
            {
                while (true)
                {
                    var result = await _application.Input.ReadAsync();
                    var buffer = result.Buffer;

                    // Get a frame from the application

                    try
                    {
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            try
                            {
                                Log.ReceivedFromApp(_logger, buffer.Length);

                                if (WebSocketCanSend(socket))
                                {
                                    await socket.SendAsync(buffer, _webSocketMessageType);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!_aborted)
                                {
                                    Log.ErrorSendingMessage(_logger, ex);
                                }
                                break;
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        _application.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                if (WebSocketCanSend(socket))
                {
                    // We're done sending, send the close frame to the client if the websocket is still open
                    await socket.CloseOutputAsync(error != null ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }

                _application.Input.Complete();

                Log.SendStopped(_logger);
            }
        }

        private static bool WebSocketCanSend(WebSocket ws)
        {
            return !(ws.State == WebSocketState.Aborted ||
                   ws.State == WebSocketState.Closed ||
                   ws.State == WebSocketState.CloseSent);
        }

        private static Uri ResolveWebSocketsUrl(Uri url)
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

            return uriBuilder.Uri;
        }

        public async Task StopAsync()
        {
            Log.TransportStopping(_logger);

            // Cancel any pending reads from the application, this should start the entire shutdown process
            _application.Input.CancelPendingRead();

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
