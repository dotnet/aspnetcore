// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Features;
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
            // Create the log scope and attempt to pass the Connection ID to it so as many logs as possible contain
            // the Connection ID metadata. If this is the negotiate request then the Connection ID for the scope will
            // be set a little later.
            var logScope = new ConnectionLogScope(GetConnectionId(context));
            using (_logger.BeginScope(logScope))
            {
                if (!await AuthorizeHelper.AuthorizeAsync(context, options.AuthorizationData))
                {
                    return;
                }

                if (HttpMethods.IsPost(context.Request.Method))
                {
                    // POST /{path}
                    await ProcessSend(context);
                }
                else if (HttpMethods.IsGet(context.Request.Method))
                {
                    // GET /{path}
                    await ExecuteEndpointAsync(context, socketDelegate, options, logScope);
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
        }

        public async Task ExecuteNegotiateAsync(HttpContext context, HttpSocketOptions options)
        {
            // Create the log scope and the scope connectionId param will be set when the connection is created.
            var logScope = new ConnectionLogScope(connectionId: string.Empty);
            using (_logger.BeginScope(logScope))
            {
                if (!await AuthorizeHelper.AuthorizeAsync(context, options.AuthorizationData))
                {
                    return;
                }

                if (HttpMethods.IsPost(context.Request.Method))
                {
                    // POST /{path}/negotiate
                    await ProcessNegotiate(context, options, logScope);
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
        }

        private async Task ExecuteEndpointAsync(HttpContext context, SocketDelegate socketDelegate, HttpSocketOptions options, ConnectionLogScope logScope)
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

                if (!await EnsureConnectionStateAsync(connection, context, TransportType.ServerSentEvents, supportedTransports, logScope, options))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                _logger.EstablishedConnection();

                // ServerSentEvents is a text protocol only
                connection.TransportCapabilities = TransferMode.Text;

                // We only need to provide the Input channel since writing to the application is handled through /send.
                var sse = new ServerSentEventsTransport(connection.Application.Reader, connection.ConnectionId, _loggerFactory);

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

                if (!await EnsureConnectionStateAsync(connection, context, TransportType.WebSockets, supportedTransports, logScope, options))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                _logger.EstablishedConnection();

                var ws = new WebSocketsTransport(options.WebSockets, connection.Application, connection, _loggerFactory);

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

                if (!await EnsureConnectionStateAsync(connection, context, TransportType.LongPolling, supportedTransports, logScope, options))
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
                        context.Response.ContentType = "plain/text";
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
                        _logger.EstablishedConnection();

                        connection.Metadata[ConnectionMetadataNames.Transport] = TransportType.LongPolling;

                        connection.ApplicationTask = ExecuteApplication(socketDelegate, connection);
                    }
                    else
                    {
                        _logger.ResumingConnection();
                    }

                    // REVIEW: Performance of this isn't great as this does a bunch of per request allocations
                    connection.Cancellation = new CancellationTokenSource();

                    var timeoutSource = new CancellationTokenSource();
                    var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(connection.Cancellation.Token, context.RequestAborted, timeoutSource.Token);

                    // Dispose these tokens when the request is over
                    context.Response.RegisterForDispose(timeoutSource);
                    context.Response.RegisterForDispose(tokenSource);

                    var longPolling = new LongPollingTransport(timeoutSource.Token, connection.Application.Reader, connection.ConnectionId, _loggerFactory);

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

                // If the application ended before the transport task then we potentially need to end the connection
                if (resultTask == connection.ApplicationTask)
                {
                    // Complete the transport (notifying it of the application error if there is one)
                    connection.Transport.Writer.TryComplete(connection.ApplicationTask.Exception);

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

                            connection.SetHttpContext(null);

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
            // Verify some initialization invariants
            // We want to be positive that the IConnectionInherentKeepAliveFeature is initialized before invoking the application, if the long polling transport is in use.
            Debug.Assert(connection.Metadata[ConnectionMetadataNames.Transport] != null, "Transport has not been initialized yet");
            Debug.Assert((TransportType?)connection.Metadata[ConnectionMetadataNames.Transport] != TransportType.LongPolling ||
                connection.Features.Get<IConnectionInherentKeepAliveFeature>() != null, "Long-polling transport is in use but IConnectionInherentKeepAliveFeature as not configured");

            // Jump onto the thread pool thread so blocking user code doesn't block the setup of the
            // connection and transport
            await AwaitableThreadPool.Yield();

            // Running this in an async method turns sync exceptions into async ones
            await socketDelegate(connection);
        }

        private Task ProcessNegotiate(HttpContext context, HttpSocketOptions options, ConnectionLogScope logScope)
        {
            // Set the allowed headers for this resource
            context.Response.Headers.AppendCommaSeparatedValues("Allow", "GET", "POST", "OPTIONS");

            context.Response.ContentType = "application/json";

            // Establish the connection
            var connection = _manager.CreateConnection();

            // Set the Connection ID on the logging scope so that logs from now on will have the
            // Connection ID metadata set.
            logScope.ConnectionId = connection.ConnectionId;

            // Get the bytes for the connection id
            var negotiateResponseBuffer = Encoding.UTF8.GetBytes(GetNegotiatePayload(connection.ConnectionId, options));

            _logger.NegotiationRequest();

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

        private static string GetConnectionId(HttpContext context) => context.Request.Query["id"];

        private async Task ProcessSend(HttpContext context)
        {
            var connection = await GetConnectionAsync(context);
            if (connection == null)
            {
                // No such connection, GetConnection already set the response status code
                return;
            }

            context.Response.ContentType = "text/plain";

            var transport = (TransportType?)connection.Metadata[ConnectionMetadataNames.Transport];
            if (transport == TransportType.WebSockets)
            {
                _logger.PostNotAllowedForWebSockets();
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                await context.Response.WriteAsync("POST requests are not allowed for WebSocket connections.");
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

            _logger.ReceivedBytes(buffer.Length);
            while (!connection.Application.Writer.TryWrite(buffer))
            {
                if (!await connection.Application.Writer.WaitToWriteAsync())
                {
                    return;
                }
            }
        }

        private async Task<bool> EnsureConnectionStateAsync(DefaultConnectionContext connection, HttpContext context, TransportType transportType, TransportType supportedTransports, ConnectionLogScope logScope, HttpSocketOptions options)
        {
            if ((supportedTransports & transportType) == 0)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                _logger.TransportNotSupported(transportType);
                await context.Response.WriteAsync($"{transportType} transport not supported by this end point type");
                return false;
            }

            var transport = (TransportType?)connection.Metadata[ConnectionMetadataNames.Transport];

            if (transport == null)
            {
                connection.Metadata[ConnectionMetadataNames.Transport] = transportType;
            }
            else if (transport != transportType)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                _logger.CannotChangeTransport(transport.Value, transportType);
                await context.Response.WriteAsync("Cannot change transports mid-connection");
                return false;
            }

            // Configure transport-specific features.
            if (transportType == TransportType.LongPolling)
            {
                connection.Features.Set<IConnectionInherentKeepAliveFeature>(new ConnectionInherentKeepAliveFeature(options.LongPolling.PollTimeout));
            }

            // Setup the connection state from the http context
            connection.User = context.User;
            connection.SetHttpContext(context);

            // this is the default setting which should be overwritten by transports that have different capabilities (e.g. SSE)
            connection.TransportCapabilities = TransferMode.Binary | TransferMode.Text;

            // Set the Connection ID on the logging scope so that logs from now on will have the
            // Connection ID metadata set.
            logScope.ConnectionId = connection.ConnectionId;

            return true;
        }

        private async Task<DefaultConnectionContext> GetConnectionAsync(HttpContext context)
        {
            var connectionId = GetConnectionId(context);

            if (StringValues.IsNullOrEmpty(connectionId))
            {
                // There's no connection ID: bad request
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Connection ID required");
                return null;
            }

            if (!_manager.TryGetConnection(connectionId, out var connection))
            {
                // No connection with that ID: Not Found
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("No Connection with that ID");
                return null;
            }

            return connection;
        }

        private async Task<DefaultConnectionContext> GetOrCreateConnectionAsync(HttpContext context)
        {
            var connectionId = GetConnectionId(context);
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
