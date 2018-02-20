// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Internal.Transports
{
    public class WebSocketsTransport : IHttpTransport
    {
        private readonly WebSocketOptions _options;
        private readonly ILogger _logger;
        private readonly IDuplexPipe _application;
        private readonly DefaultConnectionContext _connection;

        public WebSocketsTransport(WebSocketOptions options, IDuplexPipe application, DefaultConnectionContext connection, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = options;
            _application = application;
            _connection = connection;
            _logger = loggerFactory.CreateLogger<WebSocketsTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            Debug.Assert(context.WebSockets.IsWebSocketRequest, "Not a websocket request");

            using (var ws = await context.WebSockets.AcceptWebSocketAsync(_options.SubProtocol))
            {
                _logger.SocketOpened();

                try
                {
                    await ProcessSocketAsync(ws);
                }
                finally
                {
                    _logger.SocketClosed();
                }
            }
        }

        public async Task ProcessSocketAsync(WebSocket socket)
        {
            // Begin sending and receiving. Receiving must be started first because ExecuteAsync enables SendAsync.
            var receiving = StartReceiving(socket);
            var sending = StartSending(socket);

            // Wait for something to shut down.
            var trigger = await Task.WhenAny(
                receiving,
                sending);

            var failed = trigger.IsCanceled || trigger.IsFaulted;
            var task = Task.CompletedTask;
            if (trigger == receiving)
            {
                task = sending;
                _logger.WaitingForSend();
            }
            else
            {
                task = receiving;
                _logger.WaitingForClose();
            }

            await socket.CloseOutputAsync(failed ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

            var resultTask = await Task.WhenAny(task, Task.Delay(_options.CloseTimeout));

            if (resultTask != task)
            {
                _logger.CloseTimedOut();
                socket.Abort();
            }
            else
            {
                // Observe any exceptions from second completed task
                task.GetAwaiter().GetResult();
            }

            // Observe any exceptions from original completed task
            trigger.GetAwaiter().GetResult();
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
                    var isArray = memory.TryGetArray(out var arraySegment);
                    Debug.Assert(isArray);

                    // Exceptions are handled above where the send and receive tasks are being run.
                    var receiveResult = await socket.ReceiveAsync(arraySegment, CancellationToken.None);
#endif
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    _logger.MessageReceived(receiveResult.MessageType, receiveResult.Count, receiveResult.EndOfMessage);

                    _application.Output.Advance(receiveResult.Count);

                    if (receiveResult.EndOfMessage)
                    {
                        await _application.Output.FlushAsync();
                    }
                }
            }
            finally
            {
                // We're done writing
                _application.Output.Complete();
            }
        }

        private async Task StartSending(WebSocket ws)
        {
            while (true)
            {
                var result = await _application.Input.ReadAsync();
                var buffer = result.Buffer;

                // Get a frame from the application

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        try
                        {
                            _logger.SendPayload(buffer.Length);

                            var webSocketMessageType = (_connection.TransferMode == TransferMode.Binary
                                ? WebSocketMessageType.Binary
                                : WebSocketMessageType.Text);

                            if (WebSocketCanSend(ws))
                            {
                                await ws.SendAsync(buffer, webSocketMessageType);
                            }
                        }
                        catch (WebSocketException socketException) when (!WebSocketCanSend(ws))
                        {
                            // this can happen when we send the CloseFrame to the client and try to write afterwards
                            _logger.SendFailed(socketException);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorWritingFrame(ex);
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

        private static bool WebSocketCanSend(WebSocket ws)
        {
            return !(ws.State == WebSocketState.Aborted ||
                   ws.State == WebSocketState.Closed ||
                   ws.State == WebSocketState.CloseSent);
        }
    }
}
