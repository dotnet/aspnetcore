// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Log = Microsoft.AspNetCore.SignalR.HubConnectionHandlerLog;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Handles incoming connections and implements the SignalR Hub Protocol.
/// </summary>
public class HubConnectionHandler<[DynamicallyAccessedMembers(Hub.DynamicallyAccessedMembers)] THub> : ConnectionHandler where THub : Hub
{
    private readonly HubLifetimeManager<THub> _lifetimeManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HubConnectionHandler<THub>> _logger;
    private readonly IHubProtocolResolver _protocolResolver;
    private readonly HubOptions<THub> _hubOptions;
    private readonly HubOptions _globalHubOptions;
    private readonly IUserIdProvider _userIdProvider;
    private readonly HubDispatcher<THub> _dispatcher;
    private readonly bool _enableDetailedErrors;
    private readonly long? _maximumMessageSize;
    private readonly int _maxParallelInvokes;
    private readonly long _statefulReconnectBufferSize;

    // Internal for testing
    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Initializes a new instance of the <see cref="HubConnectionHandler{THub}"/> class.
    /// </summary>
    /// <param name="lifetimeManager">The hub lifetime manager.</param>
    /// <param name="protocolResolver">The protocol resolver used to resolve the protocols between client and server.</param>
    /// <param name="globalHubOptions">The global options used to initialize hubs.</param>
    /// <param name="hubOptions">Hub specific options used to initialize hubs. These options override the global options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="userIdProvider">The user ID provider used to get the user ID from a hub connection.</param>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    /// <remarks>This class is typically created via dependency injection.</remarks>
    public HubConnectionHandler(HubLifetimeManager<THub> lifetimeManager,
                                IHubProtocolResolver protocolResolver,
                                IOptions<HubOptions> globalHubOptions,
                                IOptions<HubOptions<THub>> hubOptions,
                                ILoggerFactory loggerFactory,
                                IUserIdProvider userIdProvider,
                                IServiceScopeFactory serviceScopeFactory
    )
    {
        _protocolResolver = protocolResolver;
        _lifetimeManager = lifetimeManager;
        _loggerFactory = loggerFactory;
        _hubOptions = hubOptions.Value;
        _globalHubOptions = globalHubOptions.Value;
        _logger = loggerFactory.CreateLogger<HubConnectionHandler<THub>>();
        _userIdProvider = userIdProvider;

        _enableDetailedErrors = false;
        bool disableImplicitFromServiceParameters;

        List<IHubFilter>? hubFilters = null;
        if (_hubOptions.UserHasSetValues)
        {
            _maximumMessageSize = _hubOptions.MaximumReceiveMessageSize;
            _enableDetailedErrors = _hubOptions.EnableDetailedErrors ?? _enableDetailedErrors;
            _maxParallelInvokes = _hubOptions.MaximumParallelInvocationsPerClient;
            disableImplicitFromServiceParameters = _hubOptions.DisableImplicitFromServicesParameters;
            _statefulReconnectBufferSize = _hubOptions.StatefulReconnectBufferSize;

            if (_hubOptions.HubFilters != null)
            {
                hubFilters = new List<IHubFilter>(_hubOptions.HubFilters);
            }
        }
        else
        {
            _maximumMessageSize = _globalHubOptions.MaximumReceiveMessageSize;
            _enableDetailedErrors = _globalHubOptions.EnableDetailedErrors ?? _enableDetailedErrors;
            _maxParallelInvokes = _globalHubOptions.MaximumParallelInvocationsPerClient;
            disableImplicitFromServiceParameters = _globalHubOptions.DisableImplicitFromServicesParameters;
            _statefulReconnectBufferSize = _globalHubOptions.StatefulReconnectBufferSize;

            if (_globalHubOptions.HubFilters != null)
            {
                hubFilters = new List<IHubFilter>(_globalHubOptions.HubFilters);
            }
        }

        _dispatcher = new DefaultHubDispatcher<THub>(
            serviceScopeFactory,
            new HubContext<THub>(lifetimeManager),
            _enableDetailedErrors,
            disableImplicitFromServiceParameters,
            new Logger<DefaultHubDispatcher<THub>>(loggerFactory),
            hubFilters,
            lifetimeManager);
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        // We check to see if HubOptions<THub> are set because those take precedence over global hub options.
        // Then set the keepAlive and handshakeTimeout values to the defaults in HubOptionsSetup when they were explicitly set to null.

        var supportedProtocols = _hubOptions.SupportedProtocols ?? _globalHubOptions.SupportedProtocols;
        if (supportedProtocols == null || supportedProtocols.Count == 0)
        {
            throw new InvalidOperationException("There are no supported protocols");
        }

        var handshakeTimeout = _hubOptions.HandshakeTimeout ?? _globalHubOptions.HandshakeTimeout ?? HubOptionsSetup.DefaultHandshakeTimeout;

        var contextOptions = new HubConnectionContextOptions()
        {
            KeepAliveInterval = _hubOptions.KeepAliveInterval ?? _globalHubOptions.KeepAliveInterval ?? HubOptionsSetup.DefaultKeepAliveInterval,
            ClientTimeoutInterval = _hubOptions.ClientTimeoutInterval ?? _globalHubOptions.ClientTimeoutInterval ?? HubOptionsSetup.DefaultClientTimeoutInterval,
            StreamBufferCapacity = _hubOptions.StreamBufferCapacity ?? _globalHubOptions.StreamBufferCapacity ?? HubOptionsSetup.DefaultStreamBufferCapacity,
            MaximumReceiveMessageSize = _maximumMessageSize,
            TimeProvider = TimeProvider,
            MaximumParallelInvocations = _maxParallelInvokes,
            StatefulReconnectBufferSize = _statefulReconnectBufferSize,
        };

        Log.ConnectedStarting(_logger);

        var connectionContext = new HubConnectionContext(connection, contextOptions, _loggerFactory)
        {
            OriginalActivity = Activity.Current,
        };

        // Get off the parent span.
        // This is likely the Http Request span and we want Hub method invocations to not be collected under a long running span.
        Activity.Current = null;

        var resolvedSupportedProtocols = (supportedProtocols as IReadOnlyList<string>) ?? supportedProtocols.ToList();
        if (!await connectionContext.HandshakeAsync(handshakeTimeout, resolvedSupportedProtocols, _protocolResolver, _userIdProvider, _enableDetailedErrors))
        {
            return;
        }

        // -- the connectionContext has been set up --

        try
        {
            await _lifetimeManager.OnConnectedAsync(connectionContext);
            await RunHubAsync(connectionContext);
        }
        finally
        {
            connectionContext.Cleanup();

            Log.ConnectedEnding(_logger);
            await _lifetimeManager.OnDisconnectedAsync(connectionContext);
        }
    }

    private async Task RunHubAsync(HubConnectionContext connection)
    {
        try
        {
            await _dispatcher.OnConnectedAsync(connection);
        }
        catch (Exception ex)
        {
            Log.ErrorDispatchingHubEvent(_logger, "OnConnectedAsync", ex);

            // The client shouldn't try to reconnect given an error in OnConnected.
            await SendCloseAsync(connection, ex, allowReconnect: false);

            // return instead of throw to let close message send successfully
            return;
        }

        try
        {
            await DispatchMessagesAsync(connection);
        }
        catch (OperationCanceledException)
        {
            // Don't treat OperationCanceledException as an error, it's basically a "control flow"
            // exception to stop things from running
        }
        catch (Exception ex)
        {
            Log.ErrorProcessingRequest(_logger, ex);

            await HubOnDisconnectedAsync(connection, ex);

            // return instead of throw to let close message send successfully
            return;
        }

        await HubOnDisconnectedAsync(connection, connection.CloseException);
    }

    private async Task HubOnDisconnectedAsync(HubConnectionContext connection, Exception? exception)
    {
        var disconnectException = exception;
        if (connection.CloseMessage is not null)
        {
            // If client sent a CloseMessage we don't care about any internal exceptions that may have occurred.
            // The CloseMessage indicates a graceful closure on the part of the client.
            disconnectException = null;
            exception = null;
            if (connection.CloseMessage.Error is not null)
            {
                // A bit odd for the client to send an error along with a graceful close, but just in case we should surface it in OnDisconnectedAsync
                disconnectException = new HubException(connection.CloseMessage.Error);
            }
        }

        // send close message before aborting the connection
        await SendCloseAsync(connection, exception, connection.AllowReconnect);

        // We wait on abort to complete, this is so that we can guarantee that all callbacks have fired
        // before OnDisconnectedAsync

        // Ensure the connection is aborted before firing disconnect
        await connection.AbortAsync();

        // If a client result is requested in OnDisconnectedAsync we want to avoid the SemaphoreFullException and get the better connection disconnected IOException
        _ = connection.ActiveInvocationLimit.TryAcquire();

        try
        {
            await _dispatcher.OnDisconnectedAsync(connection, disconnectException);
        }
        catch (Exception ex)
        {
            Log.ErrorDispatchingHubEvent(_logger, "OnDisconnectedAsync", ex);
            throw;
        }
    }

    private async Task SendCloseAsync(HubConnectionContext connection, Exception? exception, bool allowReconnect)
    {
        var closeMessage = CloseMessage.Empty;

        if (exception != null)
        {
            var errorMessage = ErrorMessageHelper.BuildErrorMessage("Connection closed with an error.", exception, _enableDetailedErrors);
            closeMessage = new CloseMessage(errorMessage, allowReconnect);
        }
        else if (allowReconnect)
        {
            closeMessage = new CloseMessage(error: null, allowReconnect);
        }

        try
        {
            await connection.WriteAsync(closeMessage, ignoreAbort: true);
        }
        catch (Exception ex)
        {
            Log.ErrorSendingClose(_logger, ex);
        }
    }

    private async Task DispatchMessagesAsync(HubConnectionContext connection)
    {
        var input = connection.Input;
        var protocol = connection.Protocol;
        connection.BeginClientTimeout();

        var binder = new HubConnectionBinder<THub>(_dispatcher, _lifetimeManager, connection);

        while (true)
        {
            var result = await input.ReadAsync();
            var buffer = result.Buffer;

            try
            {
                if (result.IsCanceled)
                {
                    break;
                }

                if (!buffer.IsEmpty)
                {
                    bool messageReceived = false;
                    // No message limit, just parse and dispatch
                    if (_maximumMessageSize == null)
                    {
                        while (protocol.TryParseMessage(ref buffer, binder, out var message))
                        {
                            connection.StopClientTimeout();
                            // This lets us know the timeout has stopped and we need to re-enable it after dispatching the message
                            messageReceived = true;
                            await _dispatcher.DispatchMessageAsync(connection, message);
                        }

                        if (messageReceived)
                        {
                            connection.BeginClientTimeout();
                        }
                    }
                    else
                    {
                        // We give the parser a sliding window of the default message size
                        var maxMessageSize = _maximumMessageSize.Value;

                        while (!buffer.IsEmpty)
                        {
                            var segment = buffer;
                            var overLength = false;

                            if (segment.Length > maxMessageSize)
                            {
                                segment = segment.Slice(segment.Start, maxMessageSize);
                                overLength = true;
                            }

                            if (protocol.TryParseMessage(ref segment, binder, out var message))
                            {
                                connection.StopClientTimeout();
                                // This lets us know the timeout has stopped and we need to re-enable it after dispatching the message
                                messageReceived = true;
                                await _dispatcher.DispatchMessageAsync(connection, message);
                            }
                            else if (overLength)
                            {
                                throw new InvalidDataException($"The maximum message size of {maxMessageSize}B was exceeded. The message size can be configured in AddHubOptions.");
                            }
                            else
                            {
                                // No need to update the buffer since we didn't parse anything
                                break;
                            }

                            // Update the buffer to the remaining segment
                            buffer = buffer.Slice(segment.Start);
                        }

                        if (messageReceived)
                        {
                            connection.BeginClientTimeout();
                        }
                    }
                }

                if (result.IsCompleted)
                {
                    if (!buffer.IsEmpty)
                    {
                        throw new InvalidDataException("Connection terminated while reading a message.");
                    }
                    break;
                }
            }
            finally
            {
                // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                // before yielding the read again.
                input.AdvanceTo(buffer.Start, buffer.End);
            }
        }
    }
}
