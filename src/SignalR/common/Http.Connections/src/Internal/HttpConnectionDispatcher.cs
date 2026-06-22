// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Internal.Transports;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal sealed partial class HttpConnectionDispatcher
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
    private readonly HttpConnectionsMetrics _metrics;
    private readonly ILogger _logger;
    private const int _protocolVersion = 1;

    // This should be kept in sync with CookieAuthenticationHandler
    private const string HeaderValueNoCache = "no-cache";
    private const string HeaderValueNoCacheNoStore = "no-cache, no-store";
    private const string HeaderValueEpochDate = "Thu, 01 Jan 1970 00:00:00 GMT";

    public HttpConnectionDispatcher(HttpConnectionManager manager, ILoggerFactory loggerFactory, HttpConnectionsMetrics metrics)
    {
        _manager = manager;
        _loggerFactory = loggerFactory;
        _metrics = metrics;
        _logger = _loggerFactory.CreateLogger<HttpConnectionDispatcher>();
    }

    public async Task ExecuteAsync(HttpContext context, HttpConnectionDispatcherOptions options, ConnectionDelegate connectionDelegate)
    {
        // Create the log scope and attempt to pass the Connection ID to it so as many logs as possible contain
        // the Connection ID metadata. If this is the negotiate request then the Connection ID for the scope will
        // be set a little later.

        HttpConnectionContext? connectionContext = null;
        var connectionToken = GetConnectionToken(context);

        if (!StringValues.IsNullOrEmpty(connectionToken))
        {
            // Use ToString; IsNullOrEmpty doesn't tell the compiler anything about implicit conversion to string.
            _manager.TryGetConnection(connectionToken.ToString(), out connectionContext);
        }

        var logScope = new ConnectionLogScope(connectionContext?.ConnectionId);
        using (_logger.BeginScope(logScope))
        {
            if (HttpMethods.IsPost(context.Request.Method))
            {
                // POST /{path}
                await ProcessSend(context);
            }
            else if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsConnect(context.Request.Method))
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

    public async Task ExecuteRefreshAsync(HttpContext context, HttpConnectionDispatcherOptions options)
    {
        var logScope = new ConnectionLogScope(connectionId: string.Empty);
        using (_logger.BeginScope(logScope))
        {
            if (HttpMethods.IsPost(context.Request.Method))
            {
                await ProcessRefresh(context, options, logScope);
            }
            else
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            }
        }
    }

    private async Task ProcessRefresh(HttpContext context, HttpConnectionDispatcherOptions options, ConnectionLogScope logScope)
    {
        context.Response.ContentType = "application/json";

        if (!options.EnableAuthenticationRefresh)
        {
            await WriteRefreshErrorAsync(context, StatusCodes.Status404NotFound, "refresh_disabled");
            return;
        }

        // Get connection token from query string (same pattern as send/poll/delete)
        var connectionToken = GetConnectionToken(context);
        if (StringValues.IsNullOrEmpty(connectionToken))
        {
            await WriteRefreshErrorAsync(context, StatusCodes.Status400BadRequest, "missing_connection_token");
            return;
        }

        // Look up the connection by connection token (private, not the public connectionId)
        if (!_manager.TryGetConnection(connectionToken.ToString(), out var connection))
        {
            await WriteRefreshErrorAsync(context, StatusCodes.Status404NotFound, "connection_not_found");
            return;
        }

        logScope.ConnectionId = connection.ConnectionId;

        // Negotiate v0 connections have no private token (ConnectionId == ConnectionToken), so the
        // caller would only need the public ConnectionId to POST to /refresh on behalf of any connection.
        // Require v1+ (private token) to prevent unauthorized refreshes against known connection IDs.
        if (string.Equals(connection.ConnectionId, connection.ConnectionToken, StringComparison.Ordinal))
        {
            await WriteRefreshErrorAsync(context, StatusCodes.Status400BadRequest, "unsupported_negotiate_version");
            return;
        }

        // Use the AuthenticateResult the authorization middleware already produced for this request.
        // The /refresh endpoint is stamped with the hub's authorization metadata, so the middleware
        // authenticates it against the endpoint's declared schemes and exposes the result via
        // IAuthenticateResultFeature — the same source negotiate and the connect/poll paths read. This keeps
        // /refresh consistent with those paths and avoids re-authenticating against the app's default scheme,
        // which throws or picks the wrong credential in multi-scheme apps.
        var authResult = context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult;
        if (authResult is null || !authResult.Succeeded)
        {
            await WriteRefreshErrorAsync(context, StatusCodes.Status401Unauthorized, "invalid_token");
            return;
        }

        var newPrincipal = authResult.Principal ?? context.User;
        if (HasWindowsIdentity(connection.User) || HasWindowsIdentity(newPrincipal))
        {
            await WriteRefreshErrorAsync(context, StatusCodes.Status400BadRequest, "windows_identity_not_supported");
            return;
        }

        var newExpiration = GetAuthenticationExpiration(authResult, context.User);

        if (options.OnAuthenticationRefresh is { } callback)
        {
            var accepted = await InvokeAuthenticationRefreshCallbackAsync(connection, context, callback, newPrincipal, newExpiration);
            if (!accepted)
            {
                await WriteRefreshErrorAsync(context, StatusCodes.Status403Forbidden, "permission_change_rejected");
                return;
            }
        }

        connection.UpdateUser(newPrincipal, newExpiration);

        // Compute TTL for the response
        int? tokenLifetimeSeconds = null;
        if (newExpiration != DateTimeOffset.MaxValue)
        {
            var ttl = newExpiration - DateTimeOffset.UtcNow;
            if (ttl.TotalSeconds > 0)
            {
                tokenLifetimeSeconds = (int)ttl.TotalSeconds;
            }
        }

        // Write the refresh response
        // Don't use thread static instance here because writer is used with async
        var writer = new MemoryBufferWriter();
        try
        {
            using var jsonWriter = new Utf8JsonWriter((IBufferWriter<byte>)writer);
            jsonWriter.WriteStartObject();
            if (tokenLifetimeSeconds.HasValue)
            {
                jsonWriter.WriteNumber("tokenLifetimeSeconds", tokenLifetimeSeconds.Value);
            }
            jsonWriter.WriteEndObject();
            jsonWriter.Flush();

            context.Response.ContentLength = writer.Length;
            await writer.CopyToAsync(context.Response.Body);
        }
        finally
        {
            writer.Reset();
        }
    }

    private static async Task WriteRefreshErrorAsync(HttpContext context, int statusCode, string error)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Don't use thread static instance here because writer is used with async
        var writer = new MemoryBufferWriter();
        try
        {
            using var jsonWriter = new Utf8JsonWriter((IBufferWriter<byte>)writer);
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("error", error);
            jsonWriter.WriteEndObject();
            jsonWriter.Flush();

            context.Response.ContentLength = writer.Length;
            await writer.CopyToAsync(context.Response.Body);
        }
        finally
        {
            writer.Reset();
        }
    }

    // Runs the application-provided OnAuthenticationRefresh callback for a re-authenticated principal. Shared by the
    // /refresh endpoint and the Long Polling poll path so both apply the same accept/reject policy.
    private async ValueTask<bool> InvokeAuthenticationRefreshCallbackAsync(
        HttpConnectionContext connection, HttpContext context,
        Func<AuthenticationRefreshContext, ValueTask<bool>> callback, ClaimsPrincipal newPrincipal, DateTimeOffset newExpiration)
    {
        var refreshContext = new AuthenticationRefreshContext
        {
            HttpContext = context,
            ConnectionId = connection.ConnectionId,
            PreviousUser = connection.User ?? new ClaimsPrincipal(new ClaimsIdentity()),
            NewUser = newPrincipal,
            NewExpiration = newExpiration,
        };

        if (!await callback(refreshContext))
        {
            Log.AuthenticationRefreshRejectedByCallback(_logger, connection.ConnectionId);
            return false;
        }

        return true;
    }

    private static bool ClaimsPrincipalContentEquals(ClaimsPrincipal current, ClaimsPrincipal incoming)
    {
        return SequenceEqual(current.Identities, incoming.Identities, ClaimsIdentityContentEquals);
    }

    private static bool ClaimsIdentityContentEquals(ClaimsIdentity current, ClaimsIdentity incoming)
    {
        if (!string.Equals(current.AuthenticationType, incoming.AuthenticationType, StringComparison.Ordinal)
            || !string.Equals(current.NameClaimType, incoming.NameClaimType, StringComparison.Ordinal)
            || !string.Equals(current.RoleClaimType, incoming.RoleClaimType, StringComparison.Ordinal)
            || !string.Equals(current.Label, incoming.Label, StringComparison.Ordinal))
        {
            return false;
        }

        return SequenceEqual(current.Claims, incoming.Claims, ClaimContentEquals);
    }

    private static bool ClaimContentEquals(Claim current, Claim incoming)
    {
        return string.Equals(current.Type, incoming.Type, StringComparison.Ordinal)
            && string.Equals(current.Value, incoming.Value, StringComparison.Ordinal)
            && string.Equals(current.ValueType, incoming.ValueType, StringComparison.Ordinal)
            && string.Equals(current.Issuer, incoming.Issuer, StringComparison.Ordinal)
            && string.Equals(current.OriginalIssuer, incoming.OriginalIssuer, StringComparison.Ordinal);
    }

    private static bool SequenceEqual<T>(IEnumerable<T> current, IEnumerable<T> incoming, Func<T, T, bool> equals)
    {
        using var currentEnumerator = current.GetEnumerator();
        using var incomingEnumerator = incoming.GetEnumerator();

        while (true)
        {
            var currentHasValue = currentEnumerator.MoveNext();
            if (currentHasValue != incomingEnumerator.MoveNext())
            {
                return false;
            }

            if (!currentHasValue)
            {
                return true;
            }

            if (!equals(currentEnumerator.Current, incomingEnumerator.Current))
            {
                return false;
            }
        }
    }

    private async Task ExecuteAsync(HttpContext context, ConnectionDelegate connectionDelegate, HttpConnectionDispatcherOptions options, ConnectionLogScope logScope)
    {
        // set a tag to allow Application Performance Management tools to differentiate long running requests for reporting purposes
        context.Features.Get<IHttpActivityFeature>()?.Activity.AddTag("http.long_running", "true");

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
            var sse = new ServerSentEventsServerTransport(connection.Application.Input, connection.ConnectionId, connection, _loggerFactory);

            if (connection.TryActivatePersistentConnection(connectionDelegate, sse, Task.CompletedTask, context, _logger))
            {
                await DoPersistentConnection(connection);
            }
        }
        else
        {
            // GET /{path} maps to long polling or WebSockets

            HttpConnectionContext? connection;
            var transport = HttpTransportType.LongPolling;
            if (context.WebSockets.IsWebSocketRequest)
            {
                transport = HttpTransportType.WebSockets;
                connection = await GetOrCreateConnectionAsync(context, options);

                if (connection is not null)
                {
                    Log.EstablishedConnection(_logger);

                    // Allow the reads to be canceled
                    connection.Cancellation ??= new CancellationTokenSource();
                }
            }
            else
            {
                AddNoCacheHeaders(context.Response);
                // Connection must already exist
                connection = await GetConnectionAsync(context);
            }

            if (connection == null)
            {
                // No such connection, GetConnection already set the response status code
                return;
            }

            if (!await EnsureConnectionStateAsync(connection, context, transport, supportedTransports, logScope, options))
            {
                // Bad connection state. It's already set the response status code.
                return;
            }

            if (connection.TransportType != HttpTransportType.WebSockets || connection.UseStatefulReconnect)
            {
                if (!await connection.CancelPreviousPoll(context))
                {
                    // Connection closed. It's already set the response status code.
                    return;
                }
            }

            // Create a new Tcs every poll to keep track of the poll finishing, so we can properly wait on previous polls
            var currentRequestTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var reconnectTask = Task.CompletedTask;

            switch (transport)
            {
                case HttpTransportType.None:
                    break;
                case HttpTransportType.WebSockets:
                    var isReconnect = connection.ApplicationTask is not null;
                    var ws = new WebSocketsServerTransport(options.WebSockets, connection.Application, connection, _loggerFactory);
                    if (!connection.TryActivatePersistentConnection(connectionDelegate, ws, currentRequestTcs.Task, context, _logger))
                    {
                        return;
                    }

                    if (connection.UseStatefulReconnect && isReconnect)
                    {
                        // Should call this after the transport has started, otherwise we'll be writing to a Pipe that isn't being read from
                        reconnectTask = connection.NotifyOnReconnect?.Invoke(connection.Transport.Output) ?? Task.CompletedTask;
                    }
                    break;
                case HttpTransportType.LongPolling:
                    if (!connection.TryActivateLongPollingConnection(
                        connectionDelegate, context, options.LongPolling.PollTimeout,
                        currentRequestTcs.Task, _loggerFactory, _logger))
                    {
                        return;
                    }
                    break;
                default:
                    break;
            }

            context.Features.Get<IHttpRequestTimeoutFeature>()?.DisableTimeout();

            try
            {
                await reconnectTask;
            }
            catch (Exception ex)
            {
                // MessageBuffer shouldn't throw from the callback
                // But users can technically add a callback, we don't want to trust them not to throw
                Log.NotifyOnReconnectError(_logger, ex);
            }

            var resultTask = await Task.WhenAny(connection.ApplicationTask!, connection.TransportTask!);

            try
            {
                // If the application ended before the transport task then we potentially need to end the connection
                if (resultTask == connection.ApplicationTask)
                {
                    // Complete the transport (notifying it of the application error if there is one)
                    connection.Transport.Output.Complete(connection.ApplicationTask.Exception);

                    // Wait for the transport to run
                    // Ignore exceptions, it has been logged if there is one and the application has finished
                    // So there is no one to give the exception to
                    await ((Task)connection.TransportTask!).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

                    // If the status code is a 204 it means the connection is done
                    if (context.Response.StatusCode == StatusCodes.Status204NoContent)
                    {
                        // Cancel current request to release any waiting poll and let dispose acquire the lock
                        currentRequestTcs.TrySetCanceled();

                        // We should be able to safely dispose because there's no more data being written
                        // We don't need to wait for close here since we've already waited for both sides
                        await _manager.DisposeAndRemoveAsync(connection, closeGracefully: false, HttpConnectionStopStatus.NormalClosure);
                    }
                    else
                    {
                        if (transport != HttpTransportType.LongPolling)
                        {
                            await _manager.DisposeAndRemoveAsync(connection, closeGracefully: false, HttpConnectionStopStatus.NormalClosure);
                        }
                        else
                        {
                            // Only allow repoll if we aren't removing the connection.
                            connection.MarkInactive();
                        }
                    }
                }
                else if (resultTask.IsFaulted || resultTask.IsCanceled)
                {
                    // Cancel current request to release any waiting poll and let dispose acquire the lock
                    currentRequestTcs.TrySetCanceled();
                    // We should be able to safely dispose because there's no more data being written
                    // We don't need to wait for close here since we've already waited for both sides
                    await _manager.DisposeAndRemoveAsync(connection, closeGracefully: false, HttpConnectionStopStatus.NormalClosure);
                }
                else
                {
                    // If false then the transport was ungracefully closed, this can mean a temporary network disconnection
                    // We'll mark the connection as inactive and allow the connection to reconnect if that's the case.
                    if (await connection.TransportTask!
                        // If acks aren't enabled we can close the connection immediately (not LongPolling)
                        || !connection.ClientReconnectExpected())
                    {
                        await _manager.DisposeAndRemoveAsync(connection, closeGracefully: true, HttpConnectionStopStatus.NormalClosure);
                    }
                    else
                    {
                        // Only allow repoll if we aren't removing the connection.
                        connection.MarkInactive();
                    }
                }
            }
            finally
            {
                // Artificial task queue
                // This will cause incoming polls to wait until the previous poll has finished updating internal state info
                currentRequestTcs.TrySetResult();
            }
        }
    }

    private async Task DoPersistentConnection(HttpConnectionContext connection)
    {
        // Wait for any of them to end
        await Task.WhenAny(connection.ApplicationTask!, connection.TransportTask!);

        await _manager.DisposeAndRemoveAsync(connection, closeGracefully: true, HttpConnectionStopStatus.NormalClosure);
    }

    private async Task ProcessNegotiate(HttpContext context, HttpConnectionDispatcherOptions options, ConnectionLogScope logScope)
    {
        context.Response.ContentType = "application/json";
        string? error = null;
        int clientProtocolVersion = 0;
        if (context.Request.Query.TryGetValue("NegotiateVersion", out var queryStringVersion))
        {
            // Set the negotiate response to the protocol we use.
            var queryStringVersionValue = queryStringVersion.ToString();
            if (!int.TryParse(queryStringVersionValue, out clientProtocolVersion))
            {
                error = $"The client requested a non-integer protocol version.";
                Log.InvalidNegotiateProtocolVersion(_logger, queryStringVersionValue);
            }
            else if (clientProtocolVersion < options.MinimumProtocolVersion)
            {
                error = $"The client requested version '{clientProtocolVersion}', but the server does not support this version.";
                Log.NegotiateProtocolVersionMismatch(_logger, clientProtocolVersion);
            }
            else if (clientProtocolVersion > _protocolVersion)
            {
                clientProtocolVersion = _protocolVersion;
            }
        }
        else if (options.MinimumProtocolVersion > 0)
        {
            // NegotiateVersion wasn't parsed meaning the client requests version 0.
            error = $"The client requested version '0', but the server does not support this version.";
            Log.NegotiateProtocolVersionMismatch(_logger, 0);
        }

        var useStatefulReconnect = false;
        if (options.AllowStatefulReconnects == true && context.Request.Query.TryGetValue("UseStatefulReconnect", out var useStatefulReconnectValue))
        {
            var useStatefulReconnectStringValue = useStatefulReconnectValue.ToString();
            bool.TryParse(useStatefulReconnectStringValue, out useStatefulReconnect);
        }

        // Establish the connection
        HttpConnectionContext? connection = null;
        if (error == null)
        {
            connection = CreateConnection(options, clientProtocolVersion, useStatefulReconnect);
        }

        // Set the Connection ID on the logging scope so that logs from now on will have the
        // Connection ID metadata set.
        logScope.ConnectionId = connection?.ConnectionId;

        // Don't use thread static instance here because writer is used with async
        var writer = new MemoryBufferWriter();

        try
        {
            // Get the bytes for the connection id
            WriteNegotiatePayload(writer, connection?.ConnectionId, connection?.ConnectionToken, context, options,
                clientProtocolVersion, error, useStatefulReconnect);

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

    private static void WriteNegotiatePayload(IBufferWriter<byte> writer, string? connectionId, string? connectionToken, HttpContext context, HttpConnectionDispatcherOptions options,
        int clientProtocolVersion, string? error, bool useStatefulReconnect)
    {
        var response = new NegotiationResponse();

        if (!string.IsNullOrEmpty(error))
        {
            response.Error = error;
            NegotiateProtocol.WriteResponse(response, writer);
            return;
        }

        response.Version = clientProtocolVersion;
        response.ConnectionId = connectionId;
        response.ConnectionToken = connectionToken;
        response.AvailableTransports = new List<AvailableTransport>();
        response.UseStatefulReconnect = useStatefulReconnect;

        // If authentication refresh is enabled, compute token lifetime from auth properties
        if (options.EnableAuthenticationRefresh)
        {
            var authenticateResult = context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult;
            var expiresUtc = authenticateResult is not null && !HasWindowsIdentity(authenticateResult.Principal ?? context.User)
                ? authenticateResult.Properties?.ExpiresUtc
                : null;
            if (expiresUtc.HasValue && expiresUtc.Value != DateTimeOffset.MaxValue)
            {
                var ttl = expiresUtc.Value - DateTimeOffset.UtcNow;
                if (ttl.TotalSeconds > 0)
                {
                    response.TokenLifetime = ttl;
                }
            }
        }

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

    private static StringValues GetConnectionToken(HttpContext context) => context.Request.Query["id"];

    private async Task ProcessSend(HttpContext context)
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
                    // CancelPendingFlush has canceled pending writes caused by backpressure
                    Log.ConnectionDisposed(_logger, connection.ConnectionId);

                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "text/plain";

                    // There are no writes anymore (since this is the write "loop")
                    // So it is safe to complete the writer
                    // We complete the writer here because we already have the WriteLock acquired
                    // and it's unsafe to complete outside of the lock
                    // Other code isn't guaranteed to be able to acquire the lock before another write
                    // even if CancelPendingFlush is called, and the other write could hang if there is backpressure
                    connection.Application.Output.Complete();
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

        Log.TerminatingConnection(_logger);

        // Dispose the connection, but don't wait for it. We assign it here so we can wait in tests
        connection.DisposeAndRemoveTask = _manager.DisposeAndRemoveAsync(connection, closeGracefully: false, HttpConnectionStopStatus.NormalClosure);

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

        switch (connection.TrySetTransport(transportType, _metrics))
        {
            case HttpConnectionContext.SetTransportState.Success:
                break;

            case HttpConnectionContext.SetTransportState.AlreadyActive:
                Log.ConnectionAlreadyActive(_logger, connection.ConnectionId, context.TraceIdentifier);

                // Reject the request with a 409 conflict
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "text/plain";
                return false;

            case HttpConnectionContext.SetTransportState.CannotChange:
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                Log.CannotChangeTransport(_logger, connection.TransportType, transportType);
                await context.Response.WriteAsync("Cannot change transports mid-connection");
                return false;
        }

        // Set the IHttpConnectionFeature now that we can access it.
        connection.Features.Set(context.Features.Get<IHttpConnectionFeature>());

        // Set the IConnectionEndPointFeature from the HttpContext so that real connection endpoint
        // information (LocalEndPoint and RemoteEndPoint) is available for telemetry and other uses.
        connection.Features.Set(context.Features.Get<IConnectionEndPointFeature>());

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
                CloneHttpContext(context, connection);
            }
            else
            {
                // Set the request trace identifier to the current http request handling the poll
                existing.TraceIdentifier = context.TraceIdentifier;
            }
        }
        else
        {
            connection.HttpContext = context;
        }

        var isStatefulReconnect = transportType == HttpTransportType.WebSockets
            && connection.UseStatefulReconnect
            && connection.ApplicationTask is not null;

        // On Long Polling each poll is a separate request that may carry a refreshed (or rotated) token.
        // Stateful WebSocket reconnects also carry a fresh HTTP request after the SignalR handshake, so
        // process changed principals through the authentication-refresh path instead of silently swapping
        // only the lower-level HTTP connection user.
        var newPrincipal = (transportType == HttpTransportType.LongPolling ? context.User : connection.HttpContext?.User)
            ?? new ClaimsPrincipal();
        var newExpiration = GetAuthenticationExpiration(connection, context);

        var useAuthenticationRefreshPath = false;
        var skipUserRefresh = false;
        var currentUser = connection.User;
        if (currentUser is not null)
        {
            var originalName = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var newName = newPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (originalName != newName)
            {
                // Log warning, different user
                Log.UserNameChanged(_logger, originalName, newName);
            }

            // WindowsIdentity is cloned on first poll and must not be swapped here (disposing the old
            // SafeHandles could race the application), so those connections keep the legacy behavior below.
            var authRefreshEligible = options.EnableAuthenticationRefresh
                && (transportType == HttpTransportType.LongPolling || isStatefulReconnect)
                && !HasWindowsIdentity(newPrincipal)
                && !HasWindowsIdentity(currentUser);

            if (authRefreshEligible)
            {
                // A refreshed token presents either different claims or a different expiration; a request that
                // carries the same token (same claims and expiration) is not treated as a refresh so we don't
                // run the callback or fire UserRefreshed on every poll/reconnect.
                var principalChanged = !ClaimsPrincipalContentEquals(currentUser, newPrincipal)
                    || newExpiration != connection.AuthenticationExpiration;

                // A request that arrives carrying an older token than the one already applied (e.g. a poll that
                // raced an explicit /refresh) is stale: applying it would roll the connection back to an older identity.
                var stale = newExpiration != DateTimeOffset.MaxValue
                    && connection.AuthenticationExpiration != DateTimeOffset.MaxValue
                    && newExpiration < connection.AuthenticationExpiration;

                // Auth-refresh-eligible requests never fall through to the legacy raw swap below: that swap
                // reads connection.User/HttpContext and rewrites it outside any lock, so it could roll back a
                // token a concurrent /refresh just applied. Only a genuinely changed, non-stale token runs
                // the refresh path; a stale (older) token or an idempotent (same) token leaves the connection's
                // current principal/expiration untouched.
                if (principalChanged && !stale)
                {
                    useAuthenticationRefreshPath = true;
                }
                else
                {
                    skipUserRefresh = true;
                }
            }
        }

        if (skipUserRefresh)
        {
            // Intentionally leave connection.User and AuthenticationExpiration untouched: either this request
            // carries the same token already applied, or it lost the race against a newer token (e.g. an
            // explicit /refresh). Rewriting the connection here would risk rolling it back to an older identity.
            if (currentUser is not null && connection.HttpContext is { } httpContext)
            {
                httpContext.User = currentUser;
            }
        }
        else if (useAuthenticationRefreshPath)
        {
            // Run the same authentication-refresh logic as the /refresh endpoint so the OnAuthenticationRefresh callback and
            // hub-layer refresh notification run, instead of silently swapping connection.User on the poll/reconnect request.
            if (options.OnAuthenticationRefresh is { } callback)
            {
                var accepted = await InvokeAuthenticationRefreshCallbackAsync(connection, context, callback, newPrincipal, newExpiration);
                if (!accepted)
                {
                    // If a concurrent /refresh applied a newer token while the (possibly slow) callback ran,
                    // this request's token is now stale. Don't tear down a connection that already holds a valid
                    // newer token over a rejection of an older one; just let the poll continue.
                    if (newExpiration != DateTimeOffset.MaxValue
                        && connection.AuthenticationExpiration != DateTimeOffset.MaxValue
                        && newExpiration < connection.AuthenticationExpiration)
                    {
                        return true;
                    }

                    // The application rejected the refreshed principal. Tear the connection down rather than
                    // keep serving it with a token the client has rotated away from. The connection.User and
                    // the persisted HttpContext.User are intentionally left unchanged on this path.
                    await WriteRefreshErrorAsync(context, StatusCodes.Status403Forbidden, "permission_change_rejected");
                    connection.DisposeAndRemoveTask = _manager.DisposeAndRemoveAsync(connection, closeGracefully: false, HttpConnectionStopStatus.NormalClosure);
                    return false;
                }
            }

            // UpdateUser swaps connection.User and connection.HttpContext.User under the user lock and updates
            // AuthenticationExpiration, so no separate swap or UpdateExpiration call is needed on this path.
            // If a concurrent /refresh applied a newer token between the stale check and here, UpdateUser skips
            // the rollback atomically with the swap.
            connection.UpdateUser(newPrincipal, newExpiration);
        }
        else
        {
            // For Long Polling copy the latest principal onto the persisted HttpContext, unless it's a
            // WindowsIdentity which is cloned on first poll and must not be swapped (SafeHandle disposal could
            // race the application trying to access the identity).
            if (transportType == HttpTransportType.LongPolling
                && connection.HttpContext is { } pollContext
                && !HasWindowsIdentity(context.User))
            {
                pollContext.User = context.User;
            }

            // Setup the connection state from the http context
            connection.User = connection.HttpContext?.User;

            if (transportType == HttpTransportType.LongPolling && connection.User?.Identity is WindowsIdentity)
            {
                connection.MarkUserOwned();
            }

            UpdateExpiration(connection, context);
        }

        // Set the Connection ID on the logging scope so that logs from now on will have the
        // Connection ID metadata set.
        logScope.ConnectionId = connection.ConnectionId;

        return true;
    }

    private static void UpdateExpiration(HttpConnectionContext connection, HttpContext context)
    {
        var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();

        if (authenticateResultFeature is not null)
        {
            connection.AuthenticationExpiration =
                authenticateResultFeature.AuthenticateResult is { } authenticateResult
                    ? GetAuthenticationExpiration(authenticateResult, context.User)
                    : DateTimeOffset.MaxValue;
        }
    }

    private static DateTimeOffset GetAuthenticationExpiration(HttpConnectionContext connection, HttpContext context)
    {
        var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
        if (authenticateResultFeature is null)
        {
            // No authenticate result for this request; keep the existing expiration rather than weakening
            // it to "never expires".
            return connection.AuthenticationExpiration;
        }

        return authenticateResultFeature.AuthenticateResult is { } authenticateResult
            ? GetAuthenticationExpiration(authenticateResult, context.User)
            : DateTimeOffset.MaxValue;
    }

    private static DateTimeOffset GetAuthenticationExpiration(AuthenticateResult authenticateResult, ClaimsPrincipal fallbackPrincipal)
    {
        return HasWindowsIdentity(authenticateResult.Principal ?? fallbackPrincipal)
            ? DateTimeOffset.MaxValue
            : authenticateResult.Properties?.ExpiresUtc ?? DateTimeOffset.MaxValue;
    }

    private static bool HasWindowsIdentity(ClaimsPrincipal? user)
    {
        return user?.Identities.Any(static identity => identity is WindowsIdentity) == true;
    }

    private static void CloneUser(HttpContext newContext, HttpContext oldContext)
    {
        // If the identity is a WindowsIdentity we need to clone the User.
        // This is because the WindowsIdentity uses SafeHandle's which are disposed at the end of the request
        // and accessing the identity can happen outside of the request scope.
        if (oldContext.User.Identity is WindowsIdentity windowsIdentity)
        {
            var skipFirstIdentity = false;
            if (OperatingSystem.IsWindows() && oldContext.User is WindowsPrincipal)
            {
                // We want to explicitly create a WindowsPrincipal instead of a ClaimsPrincipal
                // so methods that WindowsPrincipal overrides like 'IsInRole', work as expected.
                newContext.User = new WindowsPrincipal((WindowsIdentity)(windowsIdentity.Clone()));
                skipFirstIdentity = true;
            }
            else
            {
                newContext.User = new ClaimsPrincipal();
            }

            foreach (var identity in oldContext.User.Identities)
            {
                if (skipFirstIdentity)
                {
                    skipFirstIdentity = false;
                    continue;
                }
                newContext.User.AddIdentity(identity.Clone());
            }
        }
        else
        {
            newContext.User = oldContext.User;
        }
    }

    private static void CloneHttpContext(HttpContext context, HttpConnectionContext connection)
    {
        // The reason we're copying the base features instead of the HttpContext properties is
        // so that we can get all of the logic built into DefaultHttpContext to extract higher level
        // structure from the low level properties
        var existingRequestFeature = context.Features.GetRequiredFeature<IHttpRequestFeature>();

        var requestFeature = new HttpRequestFeature
        {
            Protocol = existingRequestFeature.Protocol,
            Method = existingRequestFeature.Method,
            Scheme = existingRequestFeature.Scheme,
            Path = existingRequestFeature.Path,
            PathBase = existingRequestFeature.PathBase,
            QueryString = existingRequestFeature.QueryString,
            RawTarget = existingRequestFeature.RawTarget
        };
        var requestHeaders = new Dictionary<string, StringValues>(existingRequestFeature.Headers.Count, StringComparer.OrdinalIgnoreCase);
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
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(Stream.Null));
        features.Set<IHttpConnectionFeature>(connectionFeature);

        // REVIEW: We could strategically look at adding other features but it might be better
        // if we expose a callback that would allow the user to preserve HttpContext properties.

        var newHttpContext = new DefaultHttpContext(features);
        newHttpContext.TraceIdentifier = context.TraceIdentifier;

        newHttpContext.SetEndpoint(context.GetEndpoint());

        CloneUser(newHttpContext, context);

        connection.ServiceScope = context.RequestServices.CreateAsyncScope();
        newHttpContext.RequestServices = connection.ServiceScope.Value.ServiceProvider;

        // REVIEW: This extends the lifetime of anything that got put into HttpContext.Items
        newHttpContext.Items = new Dictionary<object, object?>(context.Items);

        connection.HttpContext = newHttpContext;
    }

    private async Task<HttpConnectionContext?> GetConnectionAsync(HttpContext context)
    {
        var connectionToken = GetConnectionToken(context);

        if (StringValues.IsNullOrEmpty(connectionToken))
        {
            // There's no connection ID: bad request
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Connection ID required");
            return null;
        }

        // Use ToString; IsNullOrEmpty doesn't tell the compiler anything about implicit conversion to string.
        if (!_manager.TryGetConnection(connectionToken.ToString(), out var connection))
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
    private async Task<HttpConnectionContext?> GetOrCreateConnectionAsync(HttpContext context, HttpConnectionDispatcherOptions options)
    {
        var connectionToken = GetConnectionToken(context);
        HttpConnectionContext? connection;

        // There's no connection id so this is a brand new connection
        if (StringValues.IsNullOrEmpty(connectionToken))
        {
            connection = CreateConnection(options);
        }
        // Use ToString; IsNullOrEmpty doesn't tell the compiler anything about implicit conversion to string.
        else if (!_manager.TryGetConnection(connectionToken.ToString(), out connection))
        {
            // No connection with that ID: Not Found
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("No Connection with that ID");
            return null;
        }

        return connection;
    }

    private HttpConnectionContext CreateConnection(HttpConnectionDispatcherOptions options, int clientProtocolVersion = 0, bool useStatefulReconnect = false)
    {
        return _manager.CreateConnection(options, clientProtocolVersion, useStatefulReconnect);
    }

    private static void AddNoCacheHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = HeaderValueNoCacheNoStore;
        response.Headers.Pragma = HeaderValueNoCache;
        response.Headers.Expires = HeaderValueEpochDate;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
