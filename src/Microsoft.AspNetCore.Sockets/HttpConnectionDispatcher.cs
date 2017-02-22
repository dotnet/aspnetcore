// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.AspNetCore.Sockets.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

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

        public async Task ExecuteAsync<TEndPoint>(string path, HttpContext context) where TEndPoint : EndPoint
        {
            // Get the end point mapped to this http connection
            var endpoint = (EndPoint)context.RequestServices.GetRequiredService<TEndPoint>();

            if (context.Request.Path.StartsWithSegments(path + "/negotiate"))
            {
                await ProcessNegotiate(context);
            }
            else if (context.Request.Path.StartsWithSegments(path + "/send"))
            {
                await ProcessSend(context);
            }
            else
            {
                await ExecuteEndpointAsync(path, context, endpoint);
            }
        }

        private async Task ExecuteEndpointAsync(string path, HttpContext context, EndPoint endpoint)
        {
            // Server sent events transport
            if (context.Request.Path.StartsWithSegments(path + "/sse"))
            {
                // Connection must already exist
                var state = await GetConnectionAsync(context);
                if (state == null)
                {
                    // No such connection, GetConnection already set the response status code
                    return;
                }

                if (!await EnsureConnectionStateAsync(state, context, ServerSentEventsTransport.Name))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                // We only need to provide the Input channel since writing to the application is handled through /send.
                var sse = new ServerSentEventsTransport(state.Application.Input, _loggerFactory);

                await DoPersistentConnection(endpoint, sse, context, state);
            }
            else if (context.Request.Path.StartsWithSegments(path + "/ws"))
            {
                // Connection can be established lazily
                var state = await GetOrCreateConnectionAsync(context);
                if (state == null)
                {
                    // No such connection, GetOrCreateConnection already set the response status code
                    return;
                }

                if (!await EnsureConnectionStateAsync(state, context, WebSocketsTransport.Name))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                var ws = new WebSocketsTransport(state.Application, _loggerFactory);

                await DoPersistentConnection(endpoint, ws, context, state);
            }
            else if (context.Request.Path.StartsWithSegments(path + "/poll"))
            {
                // Connection must already exist
                var state = await GetConnectionAsync(context);
                if (state == null)
                {
                    // No such connection, GetConnection already set the response status code
                    return;
                }

                if (!await EnsureConnectionStateAsync(state, context, LongPollingTransport.Name))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                try
                {
                    await state.Lock.WaitAsync();

                    if (state.Status == ConnectionState.ConnectionStatus.Disposed)
                    {
                        _logger.LogDebug("Connection {connectionId} was disposed,", state.Connection.ConnectionId);

                        // The connection was disposed
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    if (state.Status == ConnectionState.ConnectionStatus.Active)
                    {
                        _logger.LogDebug("Connection {connectionId} is already active via {requestId}. Cancelling previous request.", state.Connection.ConnectionId, state.RequestId);

                        using (state.Cancellation)
                        {
                            // Cancel the previous request
                            state.Cancellation.Cancel();

                            try
                            {
                                // Wait for the previous request to drain
                                await state.TransportTask;
                            }
                            catch (OperationCanceledException)
                            {
                                // Should be a cancelled task
                            }

                            _logger.LogDebug("Previous poll cancelled for {connectionId} on {requestId}.", state.Connection.ConnectionId, state.RequestId);
                        }
                    }

                    // Mark the request identifier
                    state.RequestId = context.TraceIdentifier;

                    // Mark the connection as active
                    state.Status = ConnectionState.ConnectionStatus.Active;

                    // Raise OnConnected for new connections only since polls happen all the time
                    if (state.ApplicationTask == null)
                    {
                        _logger.LogDebug("Establishing new connection: {connectionId} on {requestId}", state.Connection.ConnectionId, state.RequestId);

                        state.Connection.Metadata["transport"] = LongPollingTransport.Name;

                        state.ApplicationTask = ExecuteApplication(endpoint, state.Connection);
                    }
                    else
                    {
                        _logger.LogDebug("Resuming existing connection: {connectionId} on {requestId}", state.Connection.ConnectionId, state.RequestId);
                    }

                    var longPolling = new LongPollingTransport(state.Application.Input, _loggerFactory);

                    state.Cancellation = new CancellationTokenSource();

                    // REVIEW: Performance of this isn't great as this does a bunch of per request allocations
                    var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(state.Cancellation.Token, context.RequestAborted);

                    // Start the transport
                    state.TransportTask = longPolling.ProcessRequestAsync(context, tokenSource.Token);
                }
                finally
                {
                    state.Lock.Release();
                }

                var resultTask = await Task.WhenAny(state.ApplicationTask, state.TransportTask);

                // If the application ended before the transport task then we need to end the connection completely
                // so there is no future polling
                if (resultTask == state.ApplicationTask)
                {
                    await _manager.DisposeAndRemoveAsync(state);
                }
                else if (!resultTask.IsCanceled)
                {
                    // Otherwise, we update the state to inactive again and wait for the next poll
                    try
                    {
                        await state.Lock.WaitAsync();

                        if (state.Status == ConnectionState.ConnectionStatus.Active)
                        {
                            // Mark the connection as inactive
                            state.LastSeenUtc = DateTime.UtcNow;

                            state.Status = ConnectionState.ConnectionStatus.Inactive;

                            state.RequestId = null;

                            // Dispose the cancellation token
                            state.Cancellation.Dispose();

                            state.Cancellation = null;
                        }
                    }
                    finally
                    {
                        state.Lock.Release();
                    }
                }
            }
        }

        private ConnectionState CreateConnection(HttpContext context)
        {
            var format =
                string.Equals(context.Request.Query["format"], "binary", StringComparison.OrdinalIgnoreCase)
                    ? MessageType.Binary
                    : MessageType.Text;

            var state = _manager.CreateConnection();
            state.Connection.User = context.User;

            // TODO: this is wrong. + how does the user add their own metadata based on HttpContext
            var formatType = (string)context.Request.Query["formatType"];
            state.Connection.Metadata["formatType"] = string.IsNullOrEmpty(formatType) ? "json" : formatType;
            return state;
        }

        private async Task DoPersistentConnection(EndPoint endpoint,
                                                  IHttpTransport transport,
                                                  HttpContext context,
                                                  ConnectionState state)
        {
            try
            {
                await state.Lock.WaitAsync();

                if (state.Status == ConnectionState.ConnectionStatus.Disposed)
                {
                    _logger.LogDebug("Connection {connectionId} was disposed,", state.Connection.ConnectionId);

                    // Connection was disposed
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                // There's already an active request
                if (state.Status == ConnectionState.ConnectionStatus.Active)
                {
                    _logger.LogDebug("Connection {connectionId} is already active via {requestId}.", state.Connection.ConnectionId, state.RequestId);

                    // Reject the request with a 409 conflict
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    return;
                }

                // Mark the connection as active
                state.Status = ConnectionState.ConnectionStatus.Active;

                // Store the request identifier
                state.RequestId = context.TraceIdentifier;

                // Call into the end point passing the connection
                state.ApplicationTask = ExecuteApplication(endpoint, state.Connection);

                // Start the transport
                state.TransportTask = transport.ProcessRequestAsync(context, context.RequestAborted);
            }
            finally
            {
                state.Lock.Release();
            }

            // Wait for any of them to end
            await Task.WhenAny(state.ApplicationTask, state.TransportTask);

            await _manager.DisposeAndRemoveAsync(state);
        }

        private async Task ExecuteApplication(EndPoint endpoint, Connection connection)
        {
            // Jump onto the thread pool thread so blocking user code doesn't block the setup of the 
            // connection and transport
            await AwaitableThreadPool.Yield();

            // Running this in an async method turns sync exceptions into async ones
            await endpoint.OnConnectedAsync(connection);
        }

        private Task ProcessNegotiate(HttpContext context)
        {
            // Establish the connection
            var state = CreateConnection(context);

            // Get the bytes for the connection id
            var connectionIdBuffer = Encoding.UTF8.GetBytes(state.Connection.ConnectionId);

            // Write it out to the response with the right content length
            context.Response.ContentLength = connectionIdBuffer.Length;
            return context.Response.Body.WriteAsync(connectionIdBuffer, 0, connectionIdBuffer.Length);
        }

        private async Task ProcessSend(HttpContext context)
        {
            var state = await GetConnectionAsync(context);
            if (state == null)
            {
                // No such connection, GetConnection already set the response status code
                return;
            }

            // Collect the message and write it to the channel
            // TODO: Need to use some kind of pooled memory here.
            byte[] buffer;
            using (var stream = new MemoryStream())
            {
                await context.Request.Body.CopyToAsync(stream);
                buffer = stream.ToArray();
            }

            var format =
                string.Equals(context.Request.Query["format"], "binary", StringComparison.OrdinalIgnoreCase)
                    ? MessageType.Binary
                    : MessageType.Text;

            var message = new Message(
                buffer,
                format,
                endOfMessage: true);

            // REVIEW: Do we want to return a specific status code here if the connection has ended?
            while (await state.Application.Output.WaitToWriteAsync())
            {
                if (state.Application.Output.TryWrite(message))
                {
                    break;
                }
            }
        }

        private async Task<bool> EnsureConnectionStateAsync(ConnectionState connectionState, HttpContext context, string transportName)
        {
            connectionState.Connection.User = context.User;

            var transport = connectionState.Connection.Metadata.Get<string>("transport");
            if (string.IsNullOrEmpty(transport))
            {
                connectionState.Connection.Metadata["transport"] = transportName;
            }
            else if (!string.Equals(transport, transportName, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Cannot change transports mid-connection");
                return false;
            }
            return true;
        }

        private async Task<ConnectionState> GetConnectionAsync(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            ConnectionState connectionState;

            if (StringValues.IsNullOrEmpty(connectionId))
            {
                // There's no connection ID: bad request
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Connection ID required");
                return null;
            }

            if (!_manager.TryGetConnection(connectionId, out connectionState))
            {
                // No connection with that ID: Not Found
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("No Connection with that ID");
                return null;
            }

            return connectionState;
        }

        private async Task<ConnectionState> GetOrCreateConnectionAsync(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            ConnectionState connectionState;

            // There's no connection id so this is a brand new connection
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                connectionState = CreateConnection(context);
            }
            else if (!_manager.TryGetConnection(connectionId, out connectionState))
            {
                // No connection with that ID: Not Found
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("No Connection with that ID");
                return null;
            }

            return connectionState;
        }
    }
}
