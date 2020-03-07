// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Handles incoming connections and implements the SignalR Hub Protocol.
    /// </summary>
    public class HubConnectionHandler<THub> : ConnectionHandler where THub : Hub
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
            if (_hubOptions.UserHasSetValues)
            {
                _maximumMessageSize = _hubOptions.MaximumReceiveMessageSize;
                _enableDetailedErrors = _hubOptions.EnableDetailedErrors ?? _enableDetailedErrors;
            }
            else
            {
                _maximumMessageSize = _globalHubOptions.MaximumReceiveMessageSize;
                _enableDetailedErrors = _globalHubOptions.EnableDetailedErrors ?? _enableDetailedErrors;
            }

            _dispatcher = new DefaultHubDispatcher<THub>(
                serviceScopeFactory,
                new HubContext<THub>(lifetimeManager),
                _enableDetailedErrors,
                new Logger<DefaultHubDispatcher<THub>>(loggerFactory));
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
            };

            Log.ConnectedStarting(_logger);

            var connectionContext = new HubConnectionContext(connection, contextOptions, _loggerFactory);

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

            await HubOnDisconnectedAsync(connection, null);
        }

        private async Task HubOnDisconnectedAsync(HubConnectionContext connection, Exception exception)
        {
            // send close message before aborting the connection
            await SendCloseAsync(connection, exception, connection.AllowReconnect);

            // We wait on abort to complete, this is so that we can guarantee that all callbacks have fired
            // before OnDisconnectedAsync

            // Ensure the connection is aborted before firing disconnect
            await connection.AbortAsync();

            try
            {
                await _dispatcher.OnDisconnectedAsync(connection, exception);
            }
            catch (Exception ex)
            {
                Log.ErrorDispatchingHubEvent(_logger, "OnDisconnectedAsync", ex);
                throw;
            }
        }

        private async Task SendCloseAsync(HubConnectionContext connection, Exception exception, bool allowReconnect)
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
                await connection.WriteAsync(closeMessage);
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


            var binder = new HubConnectionBinder<THub>(_dispatcher, connection);

            while (true)
            {
                var result = await input.ReadAsync();
                var buffer = result.Buffer;

                connection.ResetClientTimeout();

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
                                messageReceived = true;
                                connection.StopClientTimeout();
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
                                    messageReceived = true;
                                    connection.StopClientTimeout();

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

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _errorDispatchingHubEvent =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "ErrorDispatchingHubEvent"), "Error when dispatching '{HubMethod}' on hub.");

            private static readonly Action<ILogger, Exception> _errorProcessingRequest =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "ErrorProcessingRequest"), "Error when processing requests.");

            private static readonly Action<ILogger, Exception> _abortFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(3, "AbortFailed"), "Abort callback failed.");

            private static readonly Action<ILogger, Exception> _errorSendingClose =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ErrorSendingClose"), "Error when sending Close message.");

            private static readonly Action<ILogger, Exception> _connectedStarting =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "ConnectedStarting"), "OnConnectedAsync started.");

            private static readonly Action<ILogger, Exception> _connectedEnding =
                LoggerMessage.Define(LogLevel.Debug, new EventId(6, "ConnectedEnding"), "OnConnectedAsync ending.");

            public static void ErrorDispatchingHubEvent(ILogger logger, string hubMethod, Exception exception)
            {
                _errorDispatchingHubEvent(logger, hubMethod, exception);
            }

            public static void ErrorProcessingRequest(ILogger logger, Exception exception)
            {
                _errorProcessingRequest(logger, exception);
            }

            public static void AbortFailed(ILogger logger, Exception exception)
            {
                _abortFailed(logger, exception);
            }

            public static void ErrorSendingClose(ILogger logger, Exception exception)
            {
                _errorSendingClose(logger, exception);
            }

            public static void ConnectedStarting(ILogger logger)
            {
                _connectedStarting(logger, null);
            }

            public static void ConnectedEnding(ILogger logger)
            {
                _connectedEnding(logger, null);
            }
        }
    }
}
