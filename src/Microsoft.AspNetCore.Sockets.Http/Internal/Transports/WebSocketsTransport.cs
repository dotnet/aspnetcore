// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
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
        private readonly IChannelConnection<byte[]> _application;
        private readonly string _connectionId;

        public WebSocketsTransport(WebSocketOptions options, IChannelConnection<byte[]> application, string connectionId, ILoggerFactory loggerFactory)
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
            _connectionId = connectionId;
            _logger = loggerFactory.CreateLogger<WebSocketsTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            Debug.Assert(context.WebSockets.IsWebSocketRequest, "Not a websocket request");

            using (var ws = await context.WebSockets.AcceptWebSocketAsync())
            {
                _logger.SocketOpened(_connectionId);

                await ProcessSocketAsync(ws);
            }
            _logger.SocketClosed(_connectionId);
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

            // What happened?
            if (trigger == receiving)
            {
                if (receiving.IsCanceled || receiving.IsFaulted)
                {
                    // The receiver faulted or cancelled. This means the socket is probably broken. Abort the socket and propagate the exception
                    receiving.GetAwaiter().GetResult();

                    // Should never get here because GetResult above will throw
                    Debug.Fail("GetResult didn't throw?");
                    return;
                }

                // Shutting down because we received a close frame from the client.
                // Complete the input writer so that the application knows there won't be any more input.
                _logger.ClientClosed(_connectionId, receiving.Result.CloseStatus, receiving.Result.CloseStatusDescription);
                _application.Output.TryComplete();

                // Wait for the application to finish sending.
                _logger.WaitingForSend(_connectionId);
                await sending;

                // Send the server's close frame
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            else
            {
                var failed = sending.IsFaulted || _application.Input.Completion.IsFaulted;

                // The application finished sending. Close our end of the connection
                if (failed)
                {
                    _logger.FailedSending(_connectionId);
                }
                else
                {
                    _logger.FinishedSending(_connectionId);
                }
                await socket.CloseOutputAsync(!failed ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.InternalServerError, "", CancellationToken.None);

                // Now trigger the exception from the application, if there was one.
                sending.GetAwaiter().GetResult();

                _logger.WaitingForClose(_connectionId);

                // Wait for the client to close or wait for the close timeout
                var resultTask = await Task.WhenAny(receiving, Task.Delay(_options.CloseTimeout));

                // We timed out waiting for the transport to close so abort the connection so we don't attempt to write anything else
                if (resultTask != receiving)
                {
                    _logger.CloseTimedOut(_connectionId);
                    socket.Abort();
                }

                // We're done writing
                _application.Output.TryComplete();
            }
        }

        private async Task<WebSocketReceiveResult> StartReceiving(WebSocket socket)
        {
            // REVIEW: This code was copied from the client, it's highly unoptimized at the moment (especially 
            // for server logic)
            var incomingMessage = new List<ArraySegment<byte>>();
            while (true)
            {
                const int bufferSize = 4096;
                var totalBytes = 0;
                WebSocketReceiveResult receiveResult;
                do
                {
                    var buffer = new ArraySegment<byte>(new byte[bufferSize]);

                    // Exceptions are handled above where the send and receive tasks are being run.
                    receiveResult = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return receiveResult;
                    }

                    _logger.MessageReceived(_connectionId, receiveResult.MessageType, receiveResult.Count, receiveResult.EndOfMessage);

                    var truncBuffer = new ArraySegment<byte>(buffer.Array, 0, receiveResult.Count);
                    incomingMessage.Add(truncBuffer);
                    totalBytes += receiveResult.Count;
                } while (!receiveResult.EndOfMessage);

                // Making sure the message type is either text or binary
                Debug.Assert((receiveResult.MessageType == WebSocketMessageType.Binary || receiveResult.MessageType == WebSocketMessageType.Text), "Unexpected message type");

                // TODO: Check received message type against the _options.WebSocketMessageType

                byte[] messageBuffer = null;

                if (incomingMessage.Count > 1)
                {
                    messageBuffer = new byte[totalBytes];
                    var offset = 0;
                    for (var i = 0; i < incomingMessage.Count; i++)
                    {
                        Buffer.BlockCopy(incomingMessage[i].Array, 0, messageBuffer, offset, incomingMessage[i].Count);
                        offset += incomingMessage[i].Count;
                    }
                }
                else
                {
                    messageBuffer = new byte[incomingMessage[0].Count];
                    Buffer.BlockCopy(incomingMessage[0].Array, incomingMessage[0].Offset, messageBuffer, 0, incomingMessage[0].Count);
                }

                _logger.MessageToApplication(_connectionId, messageBuffer.Length);
                while (await _application.Output.WaitToWriteAsync())
                {
                    if (_application.Output.TryWrite(messageBuffer))
                    {
                        incomingMessage.Clear();
                        break;
                    }
                }
            }
        }

        private async Task StartSending(WebSocket ws)
        {
            while (await _application.Input.WaitToReadAsync())
            {
                // Get a frame from the application
                while (_application.Input.TryRead(out var buffer))
                {
                    if (buffer.Length > 0)
                    {
                        try
                        {
                            _logger.SendPayload(_connectionId, buffer.Length);

                            await ws.SendAsync(new ArraySegment<byte>(buffer), _options.WebSocketMessageType, endOfMessage: true, cancellationToken: CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorWritingFrame(_connectionId, ex);
                            break;
                        }
                    }
                }
            }
        }
    }
}
