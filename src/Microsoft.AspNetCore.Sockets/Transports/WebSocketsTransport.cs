// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets.Internal;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebSockets.Internal;

namespace Microsoft.AspNetCore.Sockets.Transports
{
    public class WebSocketsTransport : IHttpTransport
    {
        private static readonly TimeSpan _closeTimeout = TimeSpan.FromSeconds(5);
        private static readonly WebSocketAcceptContext EmptyContext = new WebSocketAcceptContext();

        private WebSocketOpcode _lastOpcode = WebSocketOpcode.Continuation;
        private bool _lastFrameIncomplete = false;

        private readonly ILogger _logger;
        private readonly IChannelConnection<Message> _connection;

        public WebSocketsTransport(IChannelConnection<Message> connection, ILoggerFactory loggerFactory)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _connection = connection;
            _logger = loggerFactory.CreateLogger<WebSocketsTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context)
        {
            var feature = context.Features.Get<IHttpWebSocketConnectionFeature>();
            if (feature == null || !feature.IsWebSocketRequest)
            {
                _logger.LogWarning("Unable to handle WebSocket request, there is no WebSocket feature available.");
                return;
            }

            using (var ws = await feature.AcceptWebSocketConnectionAsync(EmptyContext))
            {
                _logger.LogInformation("Socket opened.");

                await ProcessSocketAsync(ws);
            }
            _logger.LogInformation("Socket closed.");
        }

        public async Task ProcessSocketAsync(IWebSocketConnection socket)
        {
            // Begin sending and receiving. Receiving must be started first because ExecuteAsync enables SendAsync.
            var receiving = socket.ExecuteAsync((frame, state) => ((WebSocketsTransport)state).HandleFrame(frame), this);
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
                    // The receiver faulted or cancelled. This means the client is probably broken. Just propagate the exception and exit
                    receiving.GetAwaiter().GetResult();

                    // Should never get here because GetResult above will throw
                    Debug.Fail("GetResult didn't throw?");
                    return;
                }

                // Shutting down because we received a close frame from the client.
                // Complete the input writer so that the application knows there won't be any more input.
                _logger.LogDebug("Client closed connection with status code '{0}' ({1}). Signaling end-of-input to application", receiving.Result.Status, receiving.Result.Description);
                _connection.Output.TryComplete();

                // Wait for the application to finish sending.
                _logger.LogDebug("Waiting for the application to finish sending data");
                await sending;

                // Send the server's close frame
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure);
            }
            else
            {
                var failed = sending.IsFaulted || sending.IsCompleted;

                // The application finished sending. Close our end of the connection
                _logger.LogDebug(!failed ? "Application finished sending. Sending close frame." : "Application failed during sending. Sending InternalServerError close frame");
                await socket.CloseAsync(!failed ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.InternalServerError);

                // Now trigger the exception from the application, if there was one.
                sending.GetAwaiter().GetResult();

                _logger.LogDebug("Waiting for the client to close the socket");

                // Wait for the client to close.
                // TODO: Consider timing out here and cancelling the receive loop.
                await receiving;
                _connection.Output.TryComplete();
            }
        }

        private Task HandleFrame(WebSocketFrame frame)
        {
            // Is this a frame we care about?
            if (!frame.Opcode.IsMessage())
            {
                return TaskCache.CompletedTask;
            }

            LogFrame("Receiving", frame);

            // Determine the effective opcode based on the continuation.
            var effectiveOpcode = frame.Opcode;
            if (frame.Opcode == WebSocketOpcode.Continuation)
            {
                effectiveOpcode = _lastOpcode;
            }
            else
            {
                _lastOpcode = frame.Opcode;
            }

            // Create a Message for the frame
            var message = new Message(frame.Payload.Preserve(), effectiveOpcode == WebSocketOpcode.Binary ? Format.Binary : Format.Text, frame.EndOfMessage);

            // Write the message to the channel
            return _connection.Output.WriteAsync(message);
        }

        private void LogFrame(string action, WebSocketFrame frame)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    $"{action} frame: Opcode={frame.Opcode}, Fin={frame.EndOfMessage}, Payload={frame.Payload.Length} bytes");
            }
        }

        private async Task StartSending(IWebSocketConnection ws)
        {
            while (!_connection.Input.Completion.IsCompleted)
            {
                // Get a frame from the application
                try
                {
                    using (var message = await _connection.Input.ReadAsync())
                    {
                        if (message.Payload.Buffer.Length > 0)
                        {
                            try
                            {
                                var opcode = message.MessageFormat == Format.Binary ?
                                    WebSocketOpcode.Binary :
                                    WebSocketOpcode.Text;

                                var frame = new WebSocketFrame(
                                    endOfMessage: message.EndOfMessage,
                                    opcode: _lastFrameIncomplete ? WebSocketOpcode.Continuation : opcode,
                                    payload: message.Payload.Buffer);

                                _lastFrameIncomplete = !message.EndOfMessage;

                                LogFrame("Sending", frame);
                                await ws.SendAsync(frame);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Error writing frame to output: {0}", ex);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex.GetType().IsNested && ex.GetType().DeclaringType == typeof(Channel))
                {
                    // Gross that we have to catch this this way. See https://github.com/dotnet/corefxlab/issues/1068
                }
            }
        }
    }
}
