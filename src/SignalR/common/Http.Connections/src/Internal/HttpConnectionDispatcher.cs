// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal.Transports;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    public partial class HttpConnectionDispatcher
    {
        private static readonly AvailableTransport _webSocketAvailableTransport =
            new AvailableTransport
            {
                Transport = nameof(HttpTransportType.WebSockets),
                TransferFormats = new List<string> { nameof(TransferFormat.Text), nameof(TransferFormat.Binary) }
            };

        private static readonly AvailableTransport _serverSentEventsAvailableTransport =
            new AvailableTransport
            {
                Transport = nameof(HttpTransportType.ServerSentEvents),
                TransferFormats = new List<string> { nameof(TransferFormat.Text) }
            };

        private static readonly AvailableTransport _longPollingAvailableTransport =
            new AvailableTransport
            {
                Transport = nameof(HttpTransportType.LongPolling),
                TransferFormats = new List<string> { nameof(TransferFormat.Text), nameof(TransferFormat.Binary) }
            };

        private readonly HttpConnectionManager _manager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public HttpConnectionDispatcher(HttpConnectionManager manager, ILoggerFactory loggerFactory)
        {
            _manager = manager;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<HttpConnectionDispatcher>();
        }

        public async Task ExecuteAsync(HttpContext context, HttpConnectionDispatcherOptions options, ConnectionDelegate connectionDelegate)
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
                else if (HttpMethods.IsDelete(context.Request.Method))
                {
                    // DELETE /{path}
                    await ProcessDeleteAsync(context);
                }
                else
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
        }

        public async Task ExecuteNegotiateAsync(HttpContext context, HttpConnectionDispatcherOptions options)
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

        private async Task ExecuteAsync(HttpContext context, ConnectionDelegate connectionDelegate, HttpConnectionDispatcherOptions options, ConnectionLogScope logScope)
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

                if (!await EnsureConnectionStateAsync(connection, context, HttpTransportType.ServerSentEvents, supportedTransports, logScope, options))
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

                if (!await EnsureConnectionStateAsync(connection, context, HttpTransportType.WebSockets, supportedTransports, logScope, options))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                Log.EstablishedConnection(_logger);

                // Allow the reads to be cancelled
                connection.Cancellation = new CancellationTokenSource();

                var ws = new WebSocketsTransport(options.WebSockets, connection.Application, connection, _loggerFactory);

                await DoPersistentConnection(connectionDelegate, ws, context, connection);
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

                if (!await EnsureConnectionStateAsync(connection, context, HttpTransportType.LongPolling, supportedTransports, logScope, options))
                {
                    // Bad connection state. It's already set the response status code.
                    return;
                }

                // Create a new Tcs every poll to keep track of the poll finishing, so we can properly wait on previous polls
                var currentRequestTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                using (connection.Cancellation)
                {
                    // Cancel the previous request
                    connection.Cancellation?.Cancel();

                    try
                    {
                        // Wait for the previous request to drain
                        await connection.PreviousPollTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Previous poll canceled due to connection closing, close this poll too
                        context.Response.ContentType = "text/plain";
                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                        return;
                    }
                }

                if (!connection.TryActivateLongPollingConnection(
                        connectionDelegate, context, options.LongPolling.PollTimeout,
                        currentRequestTcs.Task, _loggerFactory, _logger))
                {
                    return;
                }

                var resultTask = await Task.WhenAny(connection.ApplicationTask, connection.TransportTask);

                try
                {
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
                            // Cancel current request to release any waiting poll and let dispose acquire the lock
                            currentRequestTcs.TrySetCanceled();

                            // We should be able to safely dispose because there's no more data being written
                            // We don't need to wait for close here since we've already waited for both sides
                            await _manager.DisposeAndRemoveAsync(connection, closeGracefully: false);

                            // Don't poll again if we've removed the connection completely
                            pollAgain = false;
                        }
                    }
                    else if (resultTask.IsFaulted)
                    {
                        // Cancel current request to release any waiting poll and let dispose acquire the lock
                        currentRequestTcs.TrySetCanceled();

                        // transport task was faulted, we should remove the connection
                        await _manager.DisposeAndRemoveAsync(connection, closeGracefully: false);

                        pollAgain = false;
                    }
                    else if (context.Response.StatusCode == StatusCodes.Status204NoContent)
                    {
                        // Don't poll if the transport task was canceled
                        pollAgain = false;
                    }

                    if (pollAgain)
                    {
                        connection.MarkInactive();
                    }
                }
                finally
                {
                    // Artificial task queue
                    // This will cause incoming polls to wait until the previous poll has finished updating internal state info
                    currentRequestTcs.TrySetResult(null);
                }
            }
        }

        private async Task DoPersistentConnection(ConnectionDelegate connectionDelegate,
                                                  IHttpTransport transport,
                                                  HttpContext context,
                                                  HttpConnectionContext connection)
        {
            if (connection.TryActivatePersistentConnection(connectionDelegate, transport, _logger))
            {
                // Wait for any of them to end
                await Task.WhenAny(connection.ApplicationTask, connection.TransportTask);

                await _manager.DisposeAndRemoveAsync(connection, closeGracefully: true);
            }
        }

        private async Task ProcessNegotiate(HttpContext context, HttpConnectionDispatcherOptions options, ConnectionLogScope logScope)
        {
            context.Response.ContentType = "application/json";

            // Establish the connection
            var connection = CreateConnection(options);

            // Set the Connection ID on the logging scope so that logs from now on will have the
            // Connection ID metadata set.
            logScope.ConnectionId = connection.ConnectionId;

            // Don't use thread static instance here because writer is used with async
            var writer = new MemoryBufferWriter();

            try
            {
                // Get the bytes for the connection id
                WriteNegotiatePayload(writer, connection.ConnectionId, context, options);

                Log.NegotiationRequest(_logger);

                // Write it out to the response with the right content length
                context.Response.ContentLength = writer.Length;
                await writer.CopyToAsync(context.Response.Body);
            }
            finally
            {
                writer.Reset();
            }
        }

        private static void WriteNegotiatePayload(IBufferWriter<byte> writer, string connectionId, HttpContext context, HttpConnectionDispatcherOptions options)
        {
            var response = new NegotiationResponse();
            response.ConnectionId = connectionId;
            response.AvailableTransports = new List<AvailableTransport>();

            if ((options.Transports & HttpTransportType.WebSockets) != 0 && ServerHasWebSockets(context.Features))
            {
                response.AvailableTransports.Add(_webSocketAvailableTransport);
            }

            if ((options.Transports & HttpTransportType.ServerSentEvents) != 0)
            {
                response.AvailableTransports.Add(_serverSentEventsAvailableTransport);
            }

            if ((options.Transports & HttpTransportType.LongPolling) != 0)
            {
                response.AvailableTransports.Add(_longPollingAvailableTransport);
            }

            NegotiateProtocol.WriteResponse(response, writer);
        }

        private static bool ServerHasWebSockets(IFeatureCollection features)
        {
            return features.Get<IHttpWebSocketFeature>() != null;
        }

        private static string GetConnectionId(HttpContext context) => context.Request.Query["id"];

        private async Task ProcessSend(HttpContext context, HttpConnectionDispatcherOptions options)
        {
            var connection = await GetConnectionAsync(context);
            if (connection == null)
            {
                // No such connection, GetConnection already set the response status code
                return;
            }

            context.Response.ContentType = "text/plain";

            if (connection.TransportType == HttpTransportType.WebSockets)
            {
                Log.PostNotAllowedForWebSockets(_logger);
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                await context.Response.WriteAsync("POST requests are not allowed for WebSocket connections.");
                return;
            }

            const int bufferSize = 4096;

            await connection.WriteLock.WaitAsync();

            try
            {
                if (connection.Status == HttpConnectionStatus.Disposed)
                {
                    Log.ConnectionDisposed(_logger, connection.ConnectionId);

                    // The connection was disposed
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "text/plain";
                    return;
                }

                try
                {
                    try
                    {
                        await context.Request.Body.CopyToAsync(connection.ApplicationStream, bufferSize);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // PipeWriter will throw an error if it is written to while dispose is in progress and the writer has been completed
                        // Dispose isn't taking WriteLock because it could be held because of backpressure, and calling CancelPendingFlush
                        // then taking the lock introduces a race condition that could lead to a deadlock
                        Log.ConnectionDisposedWhileWriteInProgress(_logger, connection.ConnectionId, ex);

                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        context.Response.ContentType = "text/plain";
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // CancelPendingFlush has canceled pending writes caused by backpresure
                        Log.ConnectionDisposed(_logger, connection.ConnectionId);

                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        context.Response.ContentType = "text/plain";
                        return;
                    }
                    catch (IOException ex)
                    {
                        // Can occur when the HTTP request is canceled by the client
                        Log.FailedToReadHttpRequestBody(_logger, connection.ConnectionId, ex);

                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        context.Response.ContentType = "text/plain";
                        return;
                    }

                    Log.ReceivedBytes(_logger, connection.ApplicationStream.Length);
                }
                finally
                {
                    // Clear the amount of read bytes so logging is accurate
                    connection.ApplicationStream.Reset();
                }
            }
            finally
            {
                connection.WriteLock.Release();
            }
        }

        private async Task ProcessDeleteAsync(HttpContext context)
        {
            var connection = await GetConnectionAsync(context);
            if (connection == null)
            {
                // No such connection, GetConnection already set the response status code
                return;
            }

            // This end point only works for long polling
            if (connection.TransportType != HttpTransportType.LongPolling)
            {
                Log.ReceivedDeleteRequestForUnsupportedTransport(_logger, connection.TransportType);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Cannot terminate this connection using the DELETE endpoint.");
                return;
            }

            Log.TerminatingConection(_logger);

            // Complete the receiving end of the pipe
            connection.Application.Output.Complete();

            // Dispose the connection gracefully, but don't wait for it. We assign it here so we can wait in tests
            connection.DisposeAndRemoveTask = _manager.DisposeAndRemoveAsync(connection, closeGracefully: true);

            context.Response.StatusCode = StatusCodes.Status202Accepted;
            context.Response.ContentType = "text/plain";
        }

        private async Task<bool> EnsureConnectionStateAsync(HttpConnectionContext connection, HttpContext context, HttpTransportType transportType, HttpTransportType supportedTransports, ConnectionLogScope logScope, HttpConnectionDispatcherOptions options)
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

            if (connection.TransportType == HttpTransportType.None)
            {
                connection.TransportType = transportType;
            }
            else if (connection.TransportType != transportType)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                Log.CannotChangeTransport(_logger, connection.TransportType, transportType);
                await context.Response.WriteAsync("Cannot change transports mid-connection");
                return false;
            }

            // Configure transport-specific features.
            if (transportType == HttpTransportType.LongPolling)
            {
                connection.HasInherentKeepAlive = true;

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

                    // Don't copy the identity if it's a windows identity
                    // We specifically clone the identity on first poll if it's a windows identity
                    // If we swapped the new User here we'd have to dispose the old identities which could race with the application
                    // trying to access the identity.
                    if (context.User.Identity is WindowsIdentity)
                    {
                        existing.User = context.User;
                    }
                }
            }
            else
            {
                connection.HttpContext = context;
            }

            // Setup the connection state from the http context
            connection.User = connection.HttpContext.User;

            // Set the Connection ID on the logging scope so that logs from now on will have the
            // Connection ID metadata set.
            logScope.ConnectionId = connection.ConnectionId;

            return true;
        }

        private static void CloneUser(HttpContext newContext, HttpContext oldContext)
        {
            if (oldContext.User.Identity is WindowsIdentity)
            {
                newContext.User = new ClaimsPrincipal();

                foreach (var identity in oldContext.User.Identities)
                {
                    newContext.User.AddIdentity(identity.Clone());
                }
            }
            else
            {
                newContext.User = oldContext.User;
            }
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
            var requestHeaders = new Dictionary<string, StringValues>(existingRequestFeature.Headers.Count, StringComparer.Ordinal);
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

            CloneUser(newHttpContext, context);

            // Making request services function property could be tricky and expensive as it would require
            // DI scope per connection. It would also mean that services resolved in middleware leading up to here
            // wouldn't be the same instance (but maybe that's fine). For now, we just return an empty service provider
            newHttpContext.RequestServices = EmptyServiceProvider.Instance;

            // REVIEW: This extends the lifetime of anything that got put into HttpContext.Items
            newHttpContext.Items = new Dictionary<object, object>(context.Items);
            return newHttpContext;
        }

        private async Task<HttpConnectionContext> GetConnectionAsync(HttpContext context)
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

        // This is only used for WebSockets connections, which can connect directly without negotiating
        private async Task<HttpConnectionContext> GetOrCreateConnectionAsync(HttpContext context, HttpConnectionDispatcherOptions options)
        {
            var connectionId = GetConnectionId(context);
            HttpConnectionContext connection;

            // There's no connection id so this is a brand new connection
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                connection = CreateConnection(options);
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

        private HttpConnectionContext CreateConnection(HttpConnectionDispatcherOptions options)
        {
            var transportPipeOptions = new PipeOptions(pauseWriterThreshold: options.TransportMaxBufferSize, resumeWriterThreshold: options.TransportMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
            var appPipeOptions = new PipeOptions(pauseWriterThreshold: options.ApplicationMaxBufferSize, resumeWriterThreshold: options.ApplicationMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);

            return _manager.CreateConnection(transportPipeOptions, appPipeOptions);
        }

        private class EmptyServiceProvider : IServiceProvider
        {
            public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
            public object GetService(Type serviceType) => null;
        }
    }
}
