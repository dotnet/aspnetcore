// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets.Internal;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.WebSockets.Internal;

namespace Microsoft.AspNetCore.Sockets
{
    public class WebSockets : IHttpTransport
    {
        private static readonly TimeSpan _closeTimeout = TimeSpan.FromSeconds(5);
        private static readonly WebSocketAcceptContext EmptyContext = new WebSocketAcceptContext();

        private readonly HttpChannel _channel;
        private readonly WebSocketOpcode _opcode;
        private readonly ILogger _logger;

        public WebSockets(Connection connection, Format format, ILoggerFactory loggerFactory)
        {
            _channel = (HttpChannel)connection.Channel;
            _opcode = format == Format.Binary ? WebSocketOpcode.Binary : WebSocketOpcode.Text;

            _logger = (ILogger)loggerFactory?.CreateLogger<WebSockets>() ?? NullLogger.Instance;
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

                // Begin sending and receiving. Receiving must be started first because ExecuteAsync enables SendAsync.
                var receiving = ws.ExecuteAsync((frame, self) => ((WebSockets)self).HandleFrame(frame), this);
                var sending = StartSending(ws);

                // Wait for something to shut down.
                var trigger = await Task.WhenAny(
                    receiving,
                    sending);

                // What happened?
                if (trigger == receiving)
                {
                    // Shutting down because we received a close frame from the client.
                    // Complete the input writer so that the application knows there won't be any more input.
                    _logger.LogDebug("Client closed connection with status code '{0}' ({1}). Signaling end-of-input to application", receiving.Result.Status, receiving.Result.Description);
                    _channel.Input.CompleteWriter();

                    // Wait for the application to finish sending.
                    _logger.LogDebug("Waiting for the application to finish sending data");
                    await sending;

                    // Send the server's close frame
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure);
                }
                else
                {
                    // The application finished sending. We're not going to keep the connection open,
                    // so close it and wait for the client to ack the close
                    _channel.Input.CompleteWriter();
                    _logger.LogDebug("Application finished sending. Sending close frame.");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure);

                    _logger.LogDebug("Waiting for the client to close the socket");
                    // TODO: Timeout.
                    await receiving;
                }
            }
            _logger.LogInformation("Socket closed.");
        }

        private Task HandleFrame(WebSocketFrame frame)
        {
            // Is this a frame we care about?
            if (!frame.Opcode.IsMessage())
            {
                return TaskCache.CompletedTask;
            }

            LogFrame("Receiving", frame);

            // Allocate space from the input channel
            var outputBuffer = _channel.Input.Alloc();

            // Append this buffer to the input channel
            _logger.LogDebug($"Appending {frame.Payload.Length} bytes to Connection channel");
            outputBuffer.Append(frame.Payload);

            return outputBuffer.FlushAsync();
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
            try
            {
                while (true)
                {
                    var result = await _channel.Output.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (buffer.IsEmpty && result.IsCompleted)
                        {
                            break;
                        }

                        // Send the buffer in a frame
                        var frame = new WebSocketFrame(
                            endOfMessage: true,
                            opcode: _opcode,
                            payload: buffer);
                        LogFrame("Sending", frame);
                        await ws.SendAsync(frame);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error writing frame to output: {0}", ex);
                        break;
                    }
                    finally
                    {
                        _channel.Output.Advance(buffer.End);
                    }
                }
            }
            finally
            {
                // No longer reading from the channel
                _channel.Output.CompleteReader();
            }
        }
    }
}
