// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
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
        private IDuplexPipe _application;
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

            if (httpOptions?.AccessTokenFactory != null)
            {
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {httpOptions.AccessTokenFactory()}");
            }

            httpOptions?.WebSocketOptions?.Invoke(_webSocket.Options);

            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<WebSocketsTransport>();
        }

        public async Task StartAsync(Uri url, IDuplexPipe application, TransferMode requestedTransferMode, IConnection connection)
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
            var sendTask = SendMessages();
            var receiveTask = ReceiveMessages();

            // TODO: Handle TCP connection errors
            // https://github.com/SignalR/SignalR/blob/1fba14fa3437e24c204dfaf8a18db3fce8acad3c/src/Microsoft.AspNet.SignalR.Core/Owin/WebSockets/WebSocketHandler.cs#L248-L251
            Running = Task.WhenAll(sendTask, receiveTask).ContinueWith(t =>
            {
                _webSocket.Dispose();
                _logger.TransportStopped(t.Exception?.InnerException);

                _application.Output.Complete(t.Exception?.InnerException);
                _application.Input.Complete();

                return t;
            }).Unwrap();
        }

        private async Task ReceiveMessages()
        {
            _logger.StartReceive();

            try
            {
                while (true)
                {
                    var memory = _application.Output.GetMemory();

#if NETCOREAPP2_1
                    var receiveResult = await _webSocket.ReceiveAsync(memory, _receiveCts.Token);
#else
                    var isArray = memory.TryGetArray(out var arraySegment);
                    Debug.Assert(isArray);

                    // Exceptions are handled above where the send and receive tasks are being run.
                    var receiveResult = await _webSocket.ReceiveAsync(arraySegment, _receiveCts.Token);
#endif

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.WebSocketClosed(_webSocket.CloseStatus);

                        if (_webSocket.CloseStatus != WebSocketCloseStatus.NormalClosure)
                        {
                            throw new InvalidOperationException($"Websocket closed with error: {_webSocket.CloseStatus}.");
                        }

                        return;
                    }

                    _logger.MessageReceived(receiveResult.MessageType, receiveResult.Count, receiveResult.EndOfMessage);

                    _application.Output.Advance(receiveResult.Count);

                    if (receiveResult.EndOfMessage)
                    {
                        await _application.Output.FlushAsync(_transportCts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.ReceiveCanceled();
            }
            finally
            {
                // We're done writing
                _logger.ReceiveStopped();
                _transportCts.Cancel();
            }
        }

        private async Task SendMessages()
        {
            _logger.SendStarted();

            var webSocketMessageType =
                Mode == TransferMode.Binary
                    ? WebSocketMessageType.Binary
                    : WebSocketMessageType.Text;

            try
            {
                while (true)
                {
                    var result = await _application.Input.ReadAsync(_transportCts.Token);
                    var buffer = result.Buffer;
                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            _logger.ReceivedFromApp(buffer.Length);

                            await _webSocket.SendAsync(buffer, webSocketMessageType, _transportCts.Token);
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.SendMessageCanceled();
                        await CloseWebSocket();
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorSendingMessage(ex);
                        await CloseWebSocket();
                        throw;
                    }
                    finally
                    {
                        _application.Input.AdvanceTo(buffer.End);
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
