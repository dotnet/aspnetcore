// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports;

internal sealed partial class WebSocketsServerTransport : IHttpTransport
{
    private readonly WebSocketOptions _options;
    private readonly ILogger _logger;
    private readonly IDuplexPipe _application;
    private readonly HttpConnectionContext _connection;
    private volatile bool _aborted;

    // Used to determine if the close was graceful or a network issue
    private bool _gracefulClose;

    public WebSocketsServerTransport(WebSocketOptions options, IDuplexPipe application, HttpConnectionContext connection, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options;
        _application = application;
        _connection = connection;

        // We create the logger with a string to preserve the logging namespace after the server side transport renames.
        _logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Connections.Internal.Transports.WebSocketsTransport");
    }

    public async Task<bool> ProcessRequestAsync(HttpContext context, CancellationToken token)
    {
        Debug.Assert(context.WebSockets.IsWebSocketRequest, "Not a websocket request");

        var subProtocol = _options.SubProtocolSelector?.Invoke(context.WebSockets.WebSocketRequestedProtocols);

        using (var ws = await context.WebSockets.AcceptWebSocketAsync(subProtocol))
        {
            Log.SocketOpened(_logger, subProtocol);

            try
            {
                await ProcessSocketAsync(ws);
            }
            finally
            {
                Log.SocketClosed(_logger);
            }
        }

        return _gracefulClose;
    }

    public async Task ProcessSocketAsync(WebSocket socket)
    {
        var ignoreFirstCancel = false;

        var receiving = StartReceiving(socket);
        var sending = StartSending(socket, ignoreFirstCancel);

        // Wait for send or receive to complete
        var trigger = await Task.WhenAny(receiving, sending);

        if (trigger == receiving)
        {
            Log.WaitingForSend(_logger);

            // We're waiting for the application to finish and there are 2 things it could be doing
            // 1. Waiting for application data
            // 2. Waiting for a websocket send to complete

            // Cancel the application so that ReadAsync yields
            _application.Input.CancelPendingRead();

            using (var delayCts = new CancellationTokenSource())
            {
                var resultTask = await Task.WhenAny(sending, Task.Delay(_options.CloseTimeout, delayCts.Token));

                if (resultTask != sending)
                {
                    // We timed out so now we're in ungraceful shutdown mode
                    Log.CloseTimedOut(_logger);

                    // Abort the websocket if we're stuck in a pending send to the client
                    _aborted = true;

                    socket.Abort();
                }
                else
                {
                    delayCts.Cancel();
                }
            }
        }
        else
        {
            Log.WaitingForClose(_logger);

            // We're waiting on the websocket to close and there are 2 things it could be doing
            // 1. Waiting for websocket data
            // 2. Waiting on a flush to complete (backpressure being applied)

            using (var delayCts = new CancellationTokenSource())
            {
                var resultTask = await Task.WhenAny(receiving, Task.Delay(_options.CloseTimeout, delayCts.Token));

                if (resultTask != receiving)
                {
                    // Abort the websocket if we're stuck in a pending receive from the client
                    _aborted = true;

                    socket.Abort();

                    // Cancel any pending flush so that we can quit
                    _application.Output.CancelPendingFlush();
                }
                else
                {
                    delayCts.Cancel();
                }
            }
        }
    }

    private async Task StartReceiving(WebSocket socket)
    {
        var token = _connection.Cancellation?.Token ?? default;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Do a 0 byte read so that idle connections don't allocate a buffer when waiting for a read
                var result = await socket.ReceiveAsync(Memory<byte>.Empty, token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _gracefulClose = true;
                    return;
                }

                var memory = _application.Output.GetMemory();

                var receiveResult = await socket.ReceiveAsync(memory, token);

                // Need to check again for netcoreapp3.0 and later because a close can happen between a 0-byte read and the actual read
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    _gracefulClose = true;
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
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            // Client has closed the WebSocket connection without completing the close handshake
            Log.ClosedPrematurely(_logger, ex);
        }
        catch (OperationCanceledException)
        {
            // Ignore aborts, don't treat them like transport errors
        }
        catch (Exception ex)
        {
            if (!_aborted && !token.IsCancellationRequested)
            {
                _gracefulClose = true;
                _application.Output.Complete(ex);
            }
        }
        finally
        {
            if (_gracefulClose)
            {
                // We're done writing
                _application.Output.Complete();
            }
        }
    }

    private async Task StartSending(WebSocket socket, bool ignoreFirstCancel)
    {
        Exception? error = null;

        try
        {
            while (true)
            {
                var result = await _application.Input.ReadAsync();
                var buffer = result.Buffer;

                // Get a frame from the application

                try
                {
                    if (result.IsCanceled && !ignoreFirstCancel)
                    {
                        break;
                    }

                    ignoreFirstCancel = false;

                    if (!buffer.IsEmpty)
                    {
                        try
                        {
                            Log.SendPayload(_logger, buffer.Length);

                            var webSocketMessageType = (_connection.ActiveFormat == TransferFormat.Binary
                                ? WebSocketMessageType.Binary
                                : WebSocketMessageType.Text);

                            if (WebSocketCanSend(socket))
                            {
                                _connection.StartSendCancellation();
                                await socket.SendAsync(buffer, webSocketMessageType, _connection.SendingToken);
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch (OperationCanceledException ex) when (ex.CancellationToken == _connection.SendingToken)
                        {
                            _gracefulClose = true;
                            // TODO: probably log
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (!_aborted)
                            {
                                Log.ErrorWritingFrame(_logger, ex);
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
                    _connection.StopSendCancellation();
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
            // Send the close frame before calling into user code
            if (WebSocketCanSend(socket))
            {
                try
                {
                    // We're done sending, send the close frame to the client if the websocket is still open
                    await socket.CloseOutputAsync(error != null ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log.ClosingWebSocketFailed(_logger, ex);
                }
            }

            if (_gracefulClose)
            {
                _application.Input.Complete();
            }

            if (error is not null)
            {
                Log.SendErrored(_logger, error);
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
