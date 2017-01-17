// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
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

                _manager.RemoveConnection(state.Connection.ConnectionId);
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

                _manager.RemoveConnection(state.Connection.ConnectionId);
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

                // Mark the connection as active
                state.Active = true;

                var longPolling = new LongPollingTransport(state.Application.Input, _loggerFactory);

                // Start the transport
                var transportTask = longPolling.ProcessRequestAsync(context);

                // Raise OnConnected for new connections only since polls happen all the time
                var endpointTask = state.Connection.Metadata.Get<Task>("endpoint");
                if (endpointTask == null)
                {
                    _logger.LogDebug("Establishing new Long Polling connection: {0}", state.Connection.ConnectionId);

                    // This will re-initialize formatType metadata, but meh...
                    state.Connection.Metadata["transport"] = LongPollingTransport.Name;

                    // REVIEW: This is super gross, this all needs to be cleaned up...
                    state.Close = async () =>
                    {
                        // Close the end point's connection
                        state.Connection.Dispose();

                        try
                        {
                            await endpointTask;
                        }
                        catch
                        {
                            // possibly invoked on a ThreadPool thread
                        }
                    };

                    endpointTask = endpoint.OnConnectedAsync(state.Connection);
                    state.Connection.Metadata["endpoint"] = endpointTask;
                }
                else
                {
                    _logger.LogDebug("Resuming existing Long Polling connection: {0}", state.Connection.ConnectionId);
                }

                var resultTask = await Task.WhenAny(endpointTask, transportTask);

                if (resultTask == endpointTask)
                {
                    // Notify the long polling transport to end
                    if (endpointTask.IsFaulted)
                    {
                        state.Connection.Transport.Output.TryComplete(endpointTask.Exception.InnerException);
                    }

                    state.Connection.Dispose();

                    await transportTask;
                }

                // Mark the connection as inactive
                state.LastSeenUtc = DateTime.UtcNow;
                state.Active = false;
            }
        }

        private ConnectionState CreateConnection(HttpContext context)
        {
            var format =
                string.Equals(context.Request.Query["format"], "binary", StringComparison.OrdinalIgnoreCase)
                    ? Format.Binary
                    : Format.Text;

            var state = _manager.CreateConnection();
            state.Connection.User = context.User;

            // TODO: this is wrong. + how does the user add their own metadata based on HttpContext
            var formatType = (string)context.Request.Query["formatType"];
            state.Connection.Metadata["formatType"] = string.IsNullOrEmpty(formatType) ? "json" : formatType;
            return state;
        }

        private static async Task DoPersistentConnection(EndPoint endpoint,
                                                         IHttpTransport transport,
                                                         HttpContext context,
                                                         ConnectionState state)
        {
            // Start the transport
            var transportTask = transport.ProcessRequestAsync(context);

            // Call into the end point passing the connection
            var endpointTask = endpoint.OnConnectedAsync(state.Connection);

            // Wait for any of them to end
            await Task.WhenAny(endpointTask, transportTask);

            // Kill the channel
            state.Dispose();

            // Wait for both
            await Task.WhenAll(endpointTask, transportTask);
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
                    ? Format.Binary
                    : Format.Text;

            var message = new Message(
                ReadableBuffer.Create(buffer).Preserve(),
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
                context.Response.StatusCode = 400;
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
