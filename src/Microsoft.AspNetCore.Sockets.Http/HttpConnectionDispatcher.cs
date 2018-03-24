// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.AspNetCore.Sockets.Internal.Transports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Sockets
{
    public partial class HttpConnectionDispatcher
    {
        private readonly HttpConnectionManager _manager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public HttpConnectionDispatcher(HttpConnectionManager manager, ILoggerFactory loggerFactory)
        {
            _manager = manager;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<HttpConnectionDispatcher>();
        }

        public async Task ExecuteAsync(HttpContext context, HttpConnectionOptions options, ConnectionDelegate connectionDelegate)
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
                    await ProcessSend(context, options);
                }
                else if (HttpMethods.IsGet(context.Request.Method))
                {
                    // GET /{path}
                    await ExecuteAsync(context, connectionDelegate, options, logScope);
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
        }

        public async Task ExecuteNegotiateAsync(HttpContext context, HttpConnectionOptions options)
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

        private async Task ExecuteAsync(HttpContext context, ConnectionDelegate connectionDelegate, HttpConnectionOptions options, ConnectionLogScope logScope)
        {
            var supportedTransports = options.Transports;

            // Server sent events transport
            // GET /{path}
            // Accept: text/event-stream
            var headers = context.Request.GetTypedHeaders();
            if (headers.Accept?.Contains(new Net.Http.Headers.MediaTypeHeaderValue("text/event-stream")) == true)
            {
                // Connection must already exist
                var connection = await GetConnectionAsync(context, options);
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

                Log.EstablishedConnection(_logger);

                // ServerSentEvents is a text protocol only
                connection.SupportedFormats = TransferFormat.Text;

                // We only need to provide the Input channel since writing to the application is handled through /send.
                var sse = new ServerSentEventsTransport(connection.Application.Input, connection.ConnectionId, _loggerFactory);

                await DoPersistentConnection(connectionDelegate, sse, context, connection);
            }
            else if (context.WebSockets.IsWebSocketRequest)
            {
                // Connection can be established lazily
                var connection = await GetOrCreateConnectionAsync(context, options);
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

                Log.EstablishedConnection(_logger);

                var ws = new WebSocketsTransport(options.WebSockets, connection.Application, connection, _loggerFactory);

                await DoPersistentConnection(connectionDelegate, ws, context, connection);
            }
            else
            {
                // GET /{path} maps to long polling

                // Connection must already exist
                var connection = await GetConnectionAsync(context, options);
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

                    if (connection.Status == HttpConnectionContext.ConnectionStatus.Disposed)
                    {
                        Log.ConnectionDisposed(_logger, connection.ConnectionId);

                        // The connection was disposed
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        context.Response.ContentType = "text/plain";
                        return;
                    }

                    if (connection.Status == HttpConnectionContext.ConnectionStatus.Active)
                    {
                        var existing = connection.GetHttpContext();
                        Log.ConnectionAlreadyActive(_logger, connection.ConnectionId, existing.TraceIdentifier);

                        using (connection.Cancellation)
                        {
                            // Cancel the previous request
                            connection.Cancellation.Cancel();

                            // Wait for the previous request to drain
                            await connection.TransportTask;

                            Log.PollCanceled(_logger, connection.ConnectionId, existing.TraceIdentifier);
                        }
                    }

                    // Mark the connection as active
                    connection.Status = HttpConnectionContext.ConnectionStatus.Active;

                    // Raise OnConnected for new connections only since polls happen all the time
                    if (connection.ApplicationTask == null)
                    {
                        Log.EstablishedConnection(_logger);

                        connection.Items[ConnectionMetadataNames.Transport] = TransportType.LongPolling;

                        connection.ApplicationTask = ExecuteApplication(connectionDelegate, connection);
                    }
                    else
                    {
                        Log.ResumingConnection(_logger);
                    }

                    // REVIEW: Performance of this isn't great as this does a bunch of per request allocations
                    connection.Cancellation = new CancellationTokenSource();

                    var timeoutSource = new CancellationTokenSource();
                    var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(connection.Cancellation.Token, context.RequestAborted, timeoutSource.Token);

                    // Dispose these tokens when the request is over
                    context.Response.RegisterForDispose(timeoutSource);
                    context.Response.RegisterForDispose(tokenSource);

                    var longPolling = new LongPollingTransport(timeoutSource.Token, connection.Application.Input, connection.ConnectionId, _loggerFactory);

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
                    connection.Transport.Output.Complete(connection.ApplicationTask.Exception);

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

                        if (connection.Status == HttpConnectionContext.ConnectionStatus.Active)
                        {
                            // Mark the connection as inactive
                            connection.LastSeenUtc = DateTime.UtcNow;

                            connection.Status = HttpConnectionContext.ConnectionStatus.Inactive;

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

        private async Task DoPersistentConnection(ConnectionDelegate connectionDelegate,
                                                  IHttpTransport transport,
                                                  HttpContext context,
                                                  HttpConnectionContext connection)
        {
            try
            {
                await connection.Lock.WaitAsync();

                if (connection.Status == HttpConnectionContext.ConnectionStatus.Disposed)
                {
                    Log.ConnectionDisposed(_logger, connection.ConnectionId);

                    // Connection was disposed
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                // There's already an active request
                if (connection.Status == HttpConnectionContext.ConnectionStatus.Active)
                {
                    Log.ConnectionAlreadyActive(_logger, connection.ConnectionId, connection.GetHttpContext().TraceIdentifier);

                    // Reject the request with a 409 conflict
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    return;
                }

                // Mark the connection as active
                connection.Status = HttpConnectionContext.ConnectionStatus.Active;

                // Call into the end point passing the connection
                connection.ApplicationTask = ExecuteApplication(connectionDelegate, connection);

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

        private async Task ExecuteApplication(ConnectionDelegate connectionDelegate, ConnectionContext connection)
        {
            // Verify some initialization invariants
            // We want to be positive that the IConnectionInherentKeepAliveFeature is initialized before invoking the application, if the long polling transport is in use.
            Debug.Assert(connection.Items[ConnectionMetadataNames.Transport] != null, "Transport has not been initialized yet");
            Debug.Assert((TransportType?)connection.Items[ConnectionMetadataNames.Transport] != TransportType.LongPolling ||
                connection.Features.Get<IConnectionInherentKeepAliveFeature>() != null, "Long-polling transport is in use but IConnectionInherentKeepAliveFeature as not configured");

            // Jump onto the thread pool thread so blocking user code doesn't block the setup of the
            // connection and transport
            await AwaitableThreadPool.Yield();

            // Running this in an async method turns sync exceptions into async ones
            await connectionDelegate(connection);
        }

        private Task ProcessNegotiate(HttpContext context, HttpConnectionOptions options, ConnectionLogScope logScope)
        {
            context.Response.ContentType = "application/json";

            // Establish the connection
            var connection = _manager.CreateConnection();

            // Set the Connection ID on the logging scope so that logs from now on will have the
            // Connection ID metadata set.
            logScope.ConnectionId = connection.ConnectionId;

            // Get the bytes for the connection id
            var negotiateResponseBuffer = Encoding.UTF8.GetBytes(GetNegotiatePayload(connection.ConnectionId, context, options));

            Log.NegotiationRequest(_logger);

            // Write it out to the response with the right content length
            context.Response.ContentLength = negotiateResponseBuffer.Length;
            return context.Response.Body.WriteAsync(negotiateResponseBuffer, 0, negotiateResponseBuffer.Length);
        }

        private static string GetNegotiatePayload(string connectionId, HttpContext context, HttpConnectionOptions options)
        {
            var sb = new StringBuilder();
            using (var jsonWriter = new JsonTextWriter(new StringWriter(sb)))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("connectionId");
                jsonWriter.WriteValue(connectionId);
                jsonWriter.WritePropertyName("availableTransports");
                jsonWriter.WriteStartArray();

                if (ServerHasWebSockets(context.Features))
                {
                    if ((options.Transports & TransportType.WebSockets) != 0)
                    {
                        WriteTransport(jsonWriter, nameof(TransportType.WebSockets), TransferFormat.Text | TransferFormat.Binary);
                    }
                }

                if ((options.Transports & TransportType.ServerSentEvents) != 0)
                {
                    WriteTransport(jsonWriter, nameof(TransportType.ServerSentEvents), TransferFormat.Text);
                }

                if ((options.Transports & TransportType.LongPolling) != 0)
                {
                    WriteTransport(jsonWriter, nameof(TransportType.LongPolling), TransferFormat.Text | TransferFormat.Binary);
                }

                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
            }

            return sb.ToString();
        }

        private static bool ServerHasWebSockets(IFeatureCollection features)
        {
            return features.Get<IHttpWebSocketFeature>() != null;
        }

        private static void WriteTransport(JsonWriter writer, string transportName, TransferFormat supportedTransferFormats)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("transport");
            writer.WriteValue(transportName);
            writer.WritePropertyName("transferFormats");
            writer.WriteStartArray();
            if ((supportedTransferFormats & TransferFormat.Binary) != 0)
            {
                writer.WriteValue(nameof(TransferFormat.Binary));
            }

            if ((supportedTransferFormats & TransferFormat.Text) != 0)
            {
                writer.WriteValue(nameof(TransferFormat.Text));
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static string GetConnectionId(HttpContext context) => context.Request.Query["id"];

        private async Task ProcessSend(HttpContext context, HttpConnectionOptions options)
        {
            var connection = await GetConnectionAsync(context, options);
            if (connection == null)
            {
                // No such connection, GetConnection already set the response status code
                return;
            }

            context.Response.ContentType = "text/plain";

            var transport = (TransportType?)connection.Items[ConnectionMetadataNames.Transport];
            if (transport == TransportType.WebSockets)
            {
                Log.PostNotAllowedForWebSockets(_logger);
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                await context.Response.WriteAsync("POST requests are not allowed for WebSocket connections.");
                return;
            }

            // Until the parsers are incremental, we buffer the entire request body before
            // flushing the buffer. Using CopyToAsync allows us to avoid allocating a single giant
            // buffer before writing.
            var pipeWriterStream = new PipeWriterStream(connection.Application.Output);
            await context.Request.Body.CopyToAsync(pipeWriterStream);

            Log.ReceivedBytes(_logger, pipeWriterStream.Length);
            await connection.Application.Output.FlushAsync();
        }

        private async Task<bool> EnsureConnectionStateAsync(HttpConnectionContext connection, HttpContext context, TransportType transportType, TransportType supportedTransports, ConnectionLogScope logScope, HttpConnectionOptions options)
        {
            if ((supportedTransports & transportType) == 0)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                Log.TransportNotSupported(_logger, transportType);
                await context.Response.WriteAsync($"{transportType} transport not supported by this end point type");
                return false;
            }

            // Set the IHttpConnectionFeature now that we can access it.
            connection.Features.Set(context.Features.Get<IHttpConnectionFeature>());

            var transport = (TransportType?)connection.Items[ConnectionMetadataNames.Transport];

            if (transport == null)
            {
                connection.Items[ConnectionMetadataNames.Transport] = transportType;
            }
            else if (transport != transportType)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                Log.CannotChangeTransport(_logger, transport.Value, transportType);
                await context.Response.WriteAsync("Cannot change transports mid-connection");
                return false;
            }

            // Setup the connection state from the http context
            connection.User = context.User;

            // Configure transport-specific features.
            if (transportType == TransportType.LongPolling)
            {
                connection.Features.Set<IConnectionInherentKeepAliveFeature>(new ConnectionInherentKeepAliveFeature(options.LongPolling.PollTimeout));

                // For long polling, the requests come and go but the connection is still alive.
                // To make the IHttpContextFeature work well, we make a copy of the relevant properties
                // to a new HttpContext. This means that it's impossible to affect the context
                // with subsequent requests.
                var existing = connection.HttpContext;
                if (existing == null)
                {
                    var httpContext = CloneHttpContext(context);
                    connection.HttpContext = httpContext;
                }
                else
                {
                    // Set the request trace identifier to the current http request handling the poll
                    existing.TraceIdentifier = context.TraceIdentifier;
                    existing.User = context.User;
                }
            }
            else
            {
                connection.HttpContext = context;
            }

            // Set the Connection ID on the logging scope so that logs from now on will have the
            // Connection ID metadata set.
            logScope.ConnectionId = connection.ConnectionId;

            return true;
        }

        private static HttpContext CloneHttpContext(HttpContext context)
        {
            // The reason we're copying the base features instead of the HttpContext properties is
            // so that we can get all of the logic built into DefaultHttpContext to extract higher level
            // structure from the low level properties
            var existingRequestFeature = context.Features.Get<IHttpRequestFeature>();

            var requestFeature = new HttpRequestFeature();
            requestFeature.Protocol = existingRequestFeature.Protocol;
            requestFeature.Method = existingRequestFeature.Method;
            requestFeature.Scheme = existingRequestFeature.Scheme;
            requestFeature.Path = existingRequestFeature.Path;
            requestFeature.PathBase = existingRequestFeature.PathBase;
            requestFeature.QueryString = existingRequestFeature.QueryString;
            requestFeature.RawTarget = existingRequestFeature.RawTarget;
            var requestHeaders = new Dictionary<string, StringValues>(existingRequestFeature.Headers.Count);
            foreach (var header in existingRequestFeature.Headers)
            {
                requestHeaders[header.Key] = header.Value;
            }
            requestFeature.Headers = new HeaderDictionary(requestHeaders);

            var existingConnectionFeature = context.Features.Get<IHttpConnectionFeature>();
            var connectionFeature = new HttpConnectionFeature();

            if (existingConnectionFeature != null)
            {
                connectionFeature.ConnectionId = existingConnectionFeature.ConnectionId;
                connectionFeature.LocalIpAddress = existingConnectionFeature.LocalIpAddress;
                connectionFeature.LocalPort = existingConnectionFeature.LocalPort;
                connectionFeature.RemoteIpAddress = existingConnectionFeature.RemoteIpAddress;
                connectionFeature.RemotePort = existingConnectionFeature.RemotePort;
            }

            // The response is a dud, you can't do anything with it anyways
            var responseFeature = new HttpResponseFeature();

            var features = new FeatureCollection();
            features.Set<IHttpRequestFeature>(requestFeature);
            features.Set<IHttpResponseFeature>(responseFeature);
            features.Set<IHttpConnectionFeature>(connectionFeature);

            // REVIEW: We could strategically look at adding other features but it might be better
            // if we expose a callback that would allow the user to preserve HttpContext properties.

            var newHttpContext = new DefaultHttpContext(features);
            newHttpContext.TraceIdentifier = context.TraceIdentifier;
            newHttpContext.User = context.User;

            // Making request services function property could be tricky and expensive as it would require
            // DI scope per connection. It would also mean that services resolved in middleware leading up to here
            // wouldn't be the same instance (but maybe that's fine). For now, we just return an empty service provider
            newHttpContext.RequestServices = EmptyServiceProvider.Instance;

            // REVIEW: This extends the lifetime of anything that got put into HttpContext.Items
            newHttpContext.Items = new Dictionary<object, object>(context.Items);
            return newHttpContext;
        }

        private async Task<HttpConnectionContext> GetConnectionAsync(HttpContext context, HttpConnectionOptions options)
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

            EnsureConnectionStateInternal(connection, options);

            return connection;
        }

        private void EnsureConnectionStateInternal(HttpConnectionContext connection, HttpConnectionOptions options)
        {
            // If the connection doesn't have a pipe yet then create one, we lazily create the pipe to save on allocations until the client actually connects
            if (connection.Transport == null)
            {
                var transportPipeOptions = new PipeOptions(pauseWriterThreshold: options.TransportMaxBufferSize, resumeWriterThreshold: options.TransportMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
                var appPipeOptions = new PipeOptions(pauseWriterThreshold: options.ApplicationMaxBufferSize, resumeWriterThreshold: options.ApplicationMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
                var pair = DuplexPipe.CreateConnectionPair(transportPipeOptions, appPipeOptions);
                connection.Transport = pair.Application;
                connection.Application = pair.Transport;
            }
        }

        // This is only used for WebSockets connections, which can connect directly without negotiating
        private async Task<HttpConnectionContext> GetOrCreateConnectionAsync(HttpContext context, HttpConnectionOptions options)
        {
            var connectionId = GetConnectionId(context);
            HttpConnectionContext connection;

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

            EnsureConnectionStateInternal(connection, options);

            return connection;
        }

        private class EmptyServiceProvider : IServiceProvider
        {
            public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
            public object GetService(Type serviceType) => null;
        }
    }
}
