// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.AspNetCore.Sockets.Internal.Transports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Sockets
{
    public class HttpConnectionDispatcher
    {
        private readonly ConnectionManager _manager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public HttpConnectionDispatcher(ConnectionManager manager, ILoggerFactory loggerFactory)
        {
            _manager = manager;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<HttpConnectionDispatcher>();
        }

        public async Task ExecuteAsync(HttpContext context, HttpSocketOptions options, SocketDelegate socketDelegate)
        {
            if (!await AuthorizeHelper.AuthorizeAsync(context, options.AuthorizationData))
            {
                return;
            }

            if (HttpMethods.IsOptions(context.Request.Method))
            {
                // OPTIONS /{path}
                await ProcessNegotiate(context, options);
            }
            else if (HttpMethods.IsPost(context.Request.Method))
            {
                // POST /{path}
                await ProcessSend(context);
            }
            else if (HttpMethods.IsGet(context.Request.Method))
            {
                // GET /{path}
                await ExecuteEndpointAsync(context, socketDelegate, options);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            }
        }

        private async Task ExecuteEndpointAsync(HttpContext context, SocketDelegate socketDelegate, HttpSocketOptions options)
        {
            var supportedTransports = options.Transports;

            // Server sent events transport
            // GET /{path}
            // Accept: text/event-stream
            var headers = context.Request.GetTypedHeaders();
            if (headers.Accept?.Contains(new Net.Http.Headers.MediaTypeHeaderValue("text/event-stream")) == true)
            {
                // Connection must already exist
                var connection = await GetConnectionAsync(context);
                if (connection == null)
                {
                    // No such connection, GetConnection already set the response status code
                    return;
                }

                _logger.EstablishedConnection(connection.ConnectionId, context.TraceIdentifier);

                if (!await EnsureConnectionStateAsync(connection, context, TransportType.ServerSentEvents, supportedTransports))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                // We only need to provide the Input channel since writing to the application is handled through /send.
                var sse = new ServerSentEventsTransport(connection.Application.In, connection.ConnectionId, _loggerFactory);

                await DoPersistentConnection(socketDelegate, sse, context, connection);
            }
            else if (context.WebSockets.IsWebSocketRequest)
            {
                // Connection can be established lazily
                var connection = await GetOrCreateConnectionAsync(context);
                if (connection == null)
                {
                    // No such connection, GetOrCreateConnection already set the response status code
                    return;
                }

                _logger.EstablishedConnection(connection.ConnectionId, context.TraceIdentifier);

                if (!await EnsureConnectionStateAsync(connection, context, TransportType.WebSockets, supportedTransports))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                var ws = new WebSocketsTransport(options.WebSockets, connection.Application, connection.ConnectionId, _loggerFactory);

                await DoPersistentConnection(socketDelegate, ws, context, connection);
            }
            else
            {
                // GET /{path} maps to long polling

                // Connection must already exist
                var connection = await GetConnectionAsync(context);
                if (connection == null)
                {
                    // No such connection, GetConnection already set the response status code
                    return;
                }

                if (!await EnsureConnectionStateAsync(connection, context, TransportType.LongPolling, supportedTransports))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                try
                {
                    await connection.Lock.WaitAsync();

                    if (connection.Status == DefaultConnectionContext.ConnectionStatus.Disposed)
                    {
                        _logger.ConnectionDisposed(connection.ConnectionId);

                        // The connection was disposed
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    if (connection.Status == DefaultConnectionContext.ConnectionStatus.Active)
                    {
                        var existing = connection.GetHttpContext();
                        _logger.ConnectionAlreadyActive(connection.ConnectionId, existing.TraceIdentifier);

                        using (connection.Cancellation)
                        {
                            // Cancel the previous request
                            connection.Cancellation.Cancel();

                            // Wait for the previous request to drain
                            await connection.TransportTask;

                            _logger.PollCanceled(connection.ConnectionId, existing.TraceIdentifier);
                        }
                    }

                    // Mark the connection as active
                    connection.Status = DefaultConnectionContext.ConnectionStatus.Active;

                    // Raise OnConnected for new connections only since polls happen all the time
                    if (connection.ApplicationTask == null)
                    {
                        _logger.EstablishedConnection(connection.ConnectionId, connection.GetHttpContext().TraceIdentifier);

                        connection.Metadata[ConnectionMetadataNames.Transport] = TransportType.LongPolling;

                        connection.ApplicationTask = ExecuteApplication(socketDelegate, connection);
                    }
                    else
                    {
                        _logger.ResumingConnection(connection.ConnectionId, connection.GetHttpContext().TraceIdentifier);
                    }

                    // REVIEW: Performance of this isn't great as this does a bunch of per request allocations
                    connection.Cancellation = new CancellationTokenSource();

                    var timeoutSource = new CancellationTokenSource();
                    var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(connection.Cancellation.Token, context.RequestAborted, timeoutSource.Token);

                    // Dispose these tokens when the request is over
                    context.Response.RegisterForDispose(timeoutSource);
                    context.Response.RegisterForDispose(tokenSource);

                    var longPolling = new LongPollingTransport(timeoutSource.Token, connection.Application.In, connection.ConnectionId, _loggerFactory);

                    // Start the transport
                    connection.TransportTask = longPolling.ProcessRequestAsync(context, tokenSource.Token);

                    // Start the timeout after we return from creating the transport task
                    timeoutSource.CancelAfter(options.LongPolling.PollTimeout);
                }
                finally
                {
                    connection.Lock.Release();
                }

                var resultTask = await Task.WhenAny(connection.ApplicationTask, connection.TransportTask);

                var pollAgain = true;

                // If the application ended before the transport task then we need to potentially need to end the
                // connection
                if (resultTask == connection.ApplicationTask)
                {
                    // Complete the transport (notifying it of the application error if there is one)
                    connection.Transport.Out.TryComplete(connection.ApplicationTask.Exception);

                    // Wait for the transport to run
                    await connection.TransportTask;

                    // If the status code is a 204 it means the connection is done
                    if (context.Response.StatusCode == StatusCodes.Status204NoContent)
                    {
                        // We should be able to safely dispose because there's no more data being written
                        await _manager.DisposeAndRemoveAsync(connection);

                        // Don't poll again if we've removed the connection completely
                        pollAgain = false;
                    }
                }
                else if (context.Response.StatusCode == StatusCodes.Status204NoContent)
                {
                    // Don't poll if the transport task was cancelled
                    pollAgain = false;
                }

                if (pollAgain)
                {
                    // Otherwise, we update the state to inactive again and wait for the next poll
                    try
                    {
                        await connection.Lock.WaitAsync();

                        if (connection.Status == DefaultConnectionContext.ConnectionStatus.Active)
                        {
                            // Mark the connection as inactive
                            connection.LastSeenUtc = DateTime.UtcNow;

                            connection.Status = DefaultConnectionContext.ConnectionStatus.Inactive;

                            connection.Metadata[ConnectionMetadataNames.HttpContext] = null;

                            // Dispose the cancellation token
                            connection.Cancellation.Dispose();

                            connection.Cancellation = null;
                        }
                    }
                    finally
                    {
                        connection.Lock.Release();
                    }
                }
            }
        }

        private async Task DoPersistentConnection(SocketDelegate socketDelegate,
                                                  IHttpTransport transport,
                                                  HttpContext context,
                                                  DefaultConnectionContext connection)
        {
            try
            {
                await connection.Lock.WaitAsync();

                if (connection.Status == DefaultConnectionContext.ConnectionStatus.Disposed)
                {
                    _logger.ConnectionDisposed(connection.ConnectionId);

                    // Connection was disposed
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                // There's already an active request
                if (connection.Status == DefaultConnectionContext.ConnectionStatus.Active)
                {
                    _logger.ConnectionAlreadyActive(connection.ConnectionId, connection.GetHttpContext().TraceIdentifier);

                    // Reject the request with a 409 conflict
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    return;
                }

                // Mark the connection as active
                connection.Status = DefaultConnectionContext.ConnectionStatus.Active;

                // Call into the end point passing the connection
                connection.ApplicationTask = ExecuteApplication(socketDelegate, connection);

                // Start the transport
                connection.TransportTask = transport.ProcessRequestAsync(context, context.RequestAborted);
            }
            finally
            {
                connection.Lock.Release();
            }

            // Wait for any of them to end
            await Task.WhenAny(connection.ApplicationTask, connection.TransportTask);

            await _manager.DisposeAndRemoveAsync(connection);
        }

        private async Task ExecuteApplication(SocketDelegate socketDelegate, ConnectionContext connection)
        {
            // Jump onto the thread pool thread so blocking user code doesn't block the setup of the
            // connection and transport
            await AwaitableThreadPool.Yield();

            // Running this in an async method turns sync exceptions into async ones
            await socketDelegate(connection);
        }

        private Task ProcessNegotiate(HttpContext context, HttpSocketOptions options)
        {
            // Set the allowed headers for this resource
            context.Response.Headers.AppendCommaSeparatedValues("Allow", "GET", "POST", "OPTIONS");

            context.Response.ContentType = "application/json";

            // Establish the connection
            var connection = _manager.CreateConnection();

            // Get the bytes for the connection id
            var negotiateResponseBuffer = Encoding.UTF8.GetBytes(GetNegotiatePayload(connection.ConnectionId, options));

            _logger.NegotiationRequest(connection.ConnectionId);

            // Write it out to the response with the right content length
            context.Response.ContentLength = negotiateResponseBuffer.Length;
            return context.Response.Body.WriteAsync(negotiateResponseBuffer, 0, negotiateResponseBuffer.Length);
        }

        private static string GetNegotiatePayload(string connectionId, HttpSocketOptions options)
        {
            var sb = new StringBuilder();
            using (var jsonWriter = new JsonTextWriter(new StringWriter(sb)))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("connectionId");
                jsonWriter.WriteValue(connectionId);
                jsonWriter.WritePropertyName("availableTransports");
                jsonWriter.WriteStartArray();
                if ((options.Transports & TransportType.WebSockets) != 0)
                {
                    jsonWriter.WriteValue(nameof(TransportType.WebSockets));
                }
                if ((options.Transports & TransportType.ServerSentEvents) != 0)
                {
                    jsonWriter.WriteValue(nameof(TransportType.ServerSentEvents));
                }
                if ((options.Transports & TransportType.LongPolling) != 0)
                {
                    jsonWriter.WriteValue(nameof(TransportType.LongPolling));
                }
                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
            }

            return sb.ToString();
        }

        private async Task ProcessSend(HttpContext context)
        {
            var connection = await GetConnectionAsync(context);
            if (connection == null)
            {
                // No such connection, GetConnection already set the response status code
                return;
            }

            // TODO: Use a pool here

            byte[] buffer;
            using (var stream = new MemoryStream())
            {
                await context.Request.Body.CopyToAsync(stream);
                await stream.FlushAsync();
                buffer = stream.ToArray();
            }

            _logger.ReceivedBytes(connection.ConnectionId, buffer.Length);
            while (!connection.Application.Out.TryWrite(buffer))
            {
                if (!await connection.Application.Out.WaitToWriteAsync())
                {
                    return;
                }
            }
        }

        private async Task<bool> EnsureConnectionStateAsync(DefaultConnectionContext connection, HttpContext context, TransportType transportType, TransportType supportedTransports)
        {
            if ((supportedTransports & transportType) == 0)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                _logger.TransportNotSupported(connection.ConnectionId, transportType);
                await context.Response.WriteAsync($"{transportType} transport not supported by this end point type");
                return false;
            }

            var transport = connection.Metadata.Get<TransportType?>(ConnectionMetadataNames.Transport);

            if (transport == null)
            {
                connection.Metadata[ConnectionMetadataNames.Transport] = transportType;
            }
            else if (transport != transportType)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                _logger.CannotChangeTransport(connection.ConnectionId, transport.Value, transportType);
                await context.Response.WriteAsync("Cannot change transports mid-connection");
                return false;
            }

            // Setup the connection state from the http context
            connection.User = context.User;
            connection.Metadata[ConnectionMetadataNames.HttpContext] = context;

            return true;
        }

        private async Task<DefaultConnectionContext> GetConnectionAsync(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];

            if (StringValues.IsNullOrEmpty(connectionId))
            {
                // There's no connection ID: bad request
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Connection ID required");
                return null;
            }

            if (!_manager.TryGetConnection(connectionId, out var connection))
            {
                // No connection with that ID: Not Found
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("No Connection with that ID");
                return null;
            }

            return connection;
        }

        private async Task<DefaultConnectionContext> GetOrCreateConnectionAsync(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            DefaultConnectionContext connection;

            // There's no connection id so this is a brand new connection
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                connection = _manager.CreateConnection();
            }
            else if (!_manager.TryGetConnection(connectionId, out connection))
            {
                // No connection with that ID: Not Found
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("No Connection with that ID");
                return null;
            }

            return connection;
        }
    }
}
