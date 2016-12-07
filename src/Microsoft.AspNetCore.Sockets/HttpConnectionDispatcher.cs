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
        private readonly PipelineFactory _pipelineFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public HttpConnectionDispatcher(ConnectionManager manager, PipelineFactory factory, ILoggerFactory loggerFactory)
        {
            _manager = manager;
            _pipelineFactory = factory;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<HttpConnectionDispatcher>();
        }

        public async Task ExecuteAsync<TEndPoint>(string path, HttpContext context) where TEndPoint : EndPoint
        {
            // Get the end point mapped to this http connection
            var endpoint = (EndPoint)context.RequestServices.GetRequiredService<TEndPoint>();

            if (context.Request.Path.StartsWithSegments(path + "/getid"))
            {
                await ProcessGetId(context, endpoint.Mode);
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
            var format =
                string.Equals(context.Request.Query["format"], "binary", StringComparison.OrdinalIgnoreCase)
                    ? Format.Binary
                    : Format.Text;

            var state = GetOrCreateConnection(context, endpoint.Mode);

            // Adapt the connection to a message-based transport if necessary, since all the HTTP transports are message-based.
            var application = GetMessagingChannel(state, format);

            // Server sent events transport
            if (context.Request.Path.StartsWithSegments(path + "/sse"))
            {
                InitializePersistentConnection(state, "sse", context, endpoint, format);

                // We only need to provide the Input channel since writing to the application is handled through /send.
                var sse = new ServerSentEventsTransport(application.Input, _loggerFactory);

                await DoPersistentConnection(endpoint, sse, context, state);

                _manager.RemoveConnection(state.Connection.ConnectionId);
            }
            else if (context.Request.Path.StartsWithSegments(path + "/ws"))
            {
                InitializePersistentConnection(state, "websockets", context, endpoint, format);

                var ws = new WebSocketsTransport(application, _loggerFactory);

                await DoPersistentConnection(endpoint, ws, context, state);

                _manager.RemoveConnection(state.Connection.ConnectionId);
            }
            else if (context.Request.Path.StartsWithSegments(path + "/poll"))
            {
                // TODO: this is wrong. + how does the user add their own metadata based on HttpContext
                var formatType = (string)context.Request.Query["formatType"];
                state.Connection.Metadata["formatType"] = string.IsNullOrEmpty(formatType) ? "json" : formatType;

                // Mark the connection as active
                state.Active = true;

                var longPolling = new LongPollingTransport(application.Input, _loggerFactory);
                RegisterLongPollingDisconnect(context, longPolling);

                // Start the transport
                var transportTask = longPolling.ProcessRequestAsync(context);

                // Raise OnConnected for new connections only since polls happen all the time
                var endpointTask = state.Connection.Metadata.Get<Task>("endpoint");
                if (endpointTask == null)
                {
                    _logger.LogDebug("Establishing new Long Polling connection: {0}", state.Connection.ConnectionId);

                    // This will re-initialize formatType metadata, but meh...
                    InitializePersistentConnection(state, "poll", context, endpoint, format);

                    // REVIEW: This is super gross, this all needs to be cleaned up...
                    state.Close = async () =>
                    {
                        try
                        {
                            await endpointTask;
                        }
                        catch
                        {
                            // possibly invoked on a ThreadPool thread
                        }

                        state.Connection.Dispose();
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
                        state.TerminateTransport(endpointTask.Exception.InnerException);
                    }
                    state.Connection.Dispose();

                    await transportTask;
                }

                // Mark the connection as inactive
                state.LastSeenUtc = DateTime.UtcNow;
                state.Active = false;
            }
        }

        private static IChannelConnection<Message> GetMessagingChannel(ConnectionState state, Format format)
        {
            if (state.Connection.Mode == ConnectionMode.Messaging)
            {
                return ((MessagingConnectionState)state).Application;
            }
            else
            {
                // We need to build an adapter
                return new FramingChannel(((StreamingConnectionState)state).Application, format);
            }
        }

        private ConnectionState InitializePersistentConnection(ConnectionState state, string transport, HttpContext context, EndPoint endpoint, Format format)
        {
            state.Connection.User = context.User;
            state.Connection.Metadata["transport"] = transport;
            state.Connection.Metadata.Format = format;

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
            // Register this transport for disconnect
            RegisterDisconnect(context, state);

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

        private static void RegisterLongPollingDisconnect(HttpContext context, LongPollingTransport transport)
        {
            // For long polling, we need to end the transport but not the overall connection so we write 0 bytes
            context.RequestAborted.Register(state => ((LongPollingTransport)state).Cancel(), transport);
        }

        private static void RegisterDisconnect(HttpContext context, ConnectionState connectionState)
        {
            // We just kill the output writing as a signal to the transport that it is done
            context.RequestAborted.Register(state => ((ConnectionState)state).Dispose(), connectionState);
        }

        private Task ProcessGetId(HttpContext context, ConnectionMode mode)
        {
            // Establish the connection
            var state = _manager.CreateConnection(mode);

            // Get the bytes for the connection id
            var connectionIdBuffer = Encoding.UTF8.GetBytes(state.Connection.ConnectionId);

            // Write it out to the response with the right content length
            context.Response.ContentLength = connectionIdBuffer.Length;
            return context.Response.Body.WriteAsync(connectionIdBuffer, 0, connectionIdBuffer.Length);
        }

        private async Task ProcessSend(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                throw new InvalidOperationException("Missing connection id");
            }

            ConnectionState state;
            if (_manager.TryGetConnection(connectionId, out state))
            {
                if (state.Connection.Mode == ConnectionMode.Streaming)
                {
                    var streamingState = (StreamingConnectionState)state;

                    await context.Request.Body.CopyToAsync(streamingState.Application.Output);
                }
                else
                {
                    // Collect the message and write it to the channel
                    // TODO: Need to use some kind of pooled memory here.
                    byte[] buffer;
                    using (var strm = new MemoryStream())
                    {
                        await context.Request.Body.CopyToAsync(strm);
                        await strm.FlushAsync();
                        buffer = strm.ToArray();
                    }

                    var format =
                        string.Equals(context.Request.Query["format"], "binary", StringComparison.OrdinalIgnoreCase)
                            ? Format.Binary
                            : Format.Text;
                    var message = new Message(
                        ReadableBuffer.Create(buffer).Preserve(),
                        format,
                        endOfMessage: true);
                    await ((MessagingConnectionState)state).Application.Output.WriteAsync(message);
                }
            }
            else
            {
                throw new InvalidOperationException("Unknown connection id");
            }
        }

        private ConnectionState GetOrCreateConnection(HttpContext context, ConnectionMode mode)
        {
            var connectionId = context.Request.Query["id"];
            ConnectionState connectionState;

            // There's no connection id so this is a brand new connection
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                connectionState = _manager.CreateConnection(mode);
            }
            else if (!_manager.TryGetConnection(connectionId, out connectionState))
            {
                throw new InvalidOperationException("Unknown connection id");
            }

            return connectionState;
        }
    }
}
