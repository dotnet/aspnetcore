// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// <see cref="CircuitRegistry"/> manages the lifetime of a <see cref="CircuitHost"/>.
    /// </summary>
    /// <remarks>
    /// Hosts start off by being registered using <see cref="CircuitHost"/>.
    ///
    /// In the simplest of cases, the client disconnects e.g. the user is done with the application and closes the browser.
    /// The server (eventually) learns of the disconnect. The host is transitioned from <see cref="ConnectedCircuits"/> to
    /// <see cref="DisconnectedCircuits"/> where it sits with an expiration time. We'll mark the associated <see cref="CircuitClientProxy"/> as disconnected
    /// so that consumers of the Circuit know of the current state.
    /// Once the entry for the host in <see cref="DisconnectedCircuits"/> expires, we'll dispose off the host.
    ///
    /// The alternate case is when the disconnect was transient, e.g. due to a network failure, and the client attempts to reconnect.
    /// We'll attempt to connect it back to the host and the preserved server state, when available. In this event, we do the opposite of
    /// what we did during disconnect - transition the host from <see cref="DisconnectedCircuits"/> to <see cref="ConnectedCircuits"/>, and transfer
    /// the <see cref="CircuitClientProxy"/> to use the new client instance that attempted to reconnect to the server. Removing the entry from
    /// <see cref="DisconnectedCircuits"/> should ensure we no longer have to concern ourselves with entry expiration.
    ///
    /// Knowing when a client disconnected is not an exact science. There's a fair possibility that a client may reconnect before the server realizes.
    /// Consequently, we have to account for reconnects and disconnects occuring simultaneously as well as appearing out of order.
    /// To manage this, we use a critical section to manage all state transitions.
    /// </remarks>
    internal class CircuitRegistry
    {
        private readonly object CircuitRegistryLock = new object();
        private readonly CircuitOptions _options;
        private readonly ILogger _logger;
        private readonly CircuitIdFactory _circuitIdFactory;
        private readonly PostEvictionCallbackRegistration _postEvictionCallback;

        public CircuitRegistry(
            IOptions<CircuitOptions> options,
            ILogger<CircuitRegistry> logger,
            CircuitIdFactory CircuitHostFactory)
        {
            _options = options.Value;
            _logger = logger;
            _circuitIdFactory = CircuitHostFactory;
            ConnectedCircuits = new ConcurrentDictionary<CircuitId, CircuitHost>();

            DisconnectedCircuits = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = _options.DisconnectedCircuitMaxRetained,
            });

            _postEvictionCallback = new PostEvictionCallbackRegistration
            {
                EvictionCallback = OnEntryEvicted,
            };
        }

        internal ConcurrentDictionary<CircuitId, CircuitHost> ConnectedCircuits { get; }

        internal MemoryCache DisconnectedCircuits { get; }

        /// <summary>
        /// Registers an active <see cref="CircuitHost"/> with the register.
        /// </summary>
        public void Register(CircuitHost circuitHost)
        {
            if (!ConnectedCircuits.TryAdd(circuitHost.CircuitId, circuitHost))
            {
                // This will likely never happen, except perhaps in unit tests, since CircuitIds are unique.
                throw new ArgumentException($"Circuit with identity {circuitHost.CircuitId} is already registered.");
            }

            // Register for unhandled exceptions from the circuit. The registry is responsible for tearing
            // down the circuit on errors.
            circuitHost.UnhandledException += CircuitHost_UnhandledException;
        }

        public virtual Task DisconnectAsync(CircuitHost circuitHost, string connectionId)
        {
            Log.CircuitDisconnectStarted(_logger, circuitHost.CircuitId, connectionId);

            Task circuitHandlerTask;
            lock (CircuitRegistryLock)
            {
                if (DisconnectCore(circuitHost, connectionId))
                {
                    circuitHandlerTask = circuitHost.Renderer.Dispatcher.InvokeAsync(() => circuitHost.OnConnectionDownAsync(default));
                }
                else
                {
                    // DisconnectCore may fail to disconnect the circuit if it was previously marked inactive or
                    // has been transferred to a new connection. Do not invoke the circuit handlers in this instance.

                    // We have to do in this instance.
                    return Task.CompletedTask;
                }
            }

            return circuitHandlerTask;
        }

        protected virtual bool DisconnectCore(CircuitHost circuitHost, string connectionId)
        {
            var circuitId = circuitHost.CircuitId;
            if (!ConnectedCircuits.TryGetValue(circuitId, out circuitHost))
            {
                Log.CircuitNotActive(_logger, circuitId);

                // Guard: The circuit might already have been marked as inactive.
                return false;
            }

            if (!string.Equals(circuitHost.Client.ConnectionId, connectionId, StringComparison.Ordinal))
            {
                Log.CircuitConnectedToDifferentConnection(_logger, circuitId, circuitHost.Client.ConnectionId);

                // The circuit is associated with a different connection. One way this could happen is when
                // the client reconnects with a new connection before the OnDisconnect for the older
                // connection is executed. Do nothing
                return false;
            }

            var result = ConnectedCircuits.TryRemove(circuitId, out circuitHost);
            Debug.Assert(result, "This operation operates inside of a lock. We expect the previously inspected value to be still here.");

            circuitHost.Client.SetDisconnected();
            RegisterDisconnectedCircuit(circuitHost);

            Log.CircuitMarkedDisconnected(_logger, circuitId);

            return true;
        }

        public void RegisterDisconnectedCircuit(CircuitHost circuitHost)
        {
            var cancellationTokenSource = new CancellationTokenSource(_options.DisconnectedCircuitRetentionPeriod);
            var entryOptions = new MemoryCacheEntryOptions
            {
                Size = 1,
                PostEvictionCallbacks = { _postEvictionCallback },
                ExpirationTokens =
                {
                    new CancellationChangeToken(cancellationTokenSource.Token),
                },
            };

            var entry = new DisconnectedCircuitEntry(circuitHost, cancellationTokenSource);
            DisconnectedCircuits.Set(circuitHost.CircuitId.Secret, entry, entryOptions);
        }

        // ConnectAsync is called from the CircuitHub - but the error handling story is a little bit complicated.
        // We return the circuit from this method, but need to clean up the circuit on failure. So we don't want to
        // throw from this method because we don't want to return a *failed* circuit.
        //
        // The solution is to handle exceptions here, and then return null to represent failure.
        //
        // 1. If the circuit is not found return null
        // 2. If the circuit is found, but fails to connect, we need to dispose it here and return null
        // 3. If everything goes well, return the circuit.
        public virtual async Task<CircuitHost> ConnectAsync(CircuitId circuitId, IClientProxy clientProxy, string connectionId, CancellationToken cancellationToken)
        {
            Log.CircuitConnectStarted(_logger, circuitId);

            CircuitHost circuitHost;
            bool previouslyConnected;

            Task circuitHandlerTask;

            // We don't expect any of the logic inside the lock to throw, or run user code.
            lock (CircuitRegistryLock)
            {
                // Transition the host from disconnected to connected if it's available. In this critical section, we return
                // an existing host if it's currently considered connected or transition a disconnected host to connected.
                // Transferring also wires up the client to the new set.
                (circuitHost, previouslyConnected) = ConnectCore(circuitId, clientProxy, connectionId);

                if (circuitHost == null)
                {
                    Log.FailedToFindCircuit(_logger, circuitId);
                    // Failed to find a matching circuit. Nothing to do here.
                    return null;
                }

                // CircuitHandler events do not need to be executed inside the critical section, however we
                // a) do not want concurrent execution of handler events i.e. a  OnConnectionDownAsync occuring in tandem with a OnConnectionUpAsync for a single circuit.
                // b) out of order connection-up \ connection-down events e.g. a client that disconnects as soon it finishes reconnecting.

                // Dispatch the circuit handlers inside the sync context to ensure the order of execution. CircuitHost executes circuit handlers inside of
                // the sync context.
                circuitHandlerTask = circuitHost.Renderer.Dispatcher.InvokeAsync(async () =>
                {
                    if (previouslyConnected)
                    {
                        // During reconnects, we may transition from Connect->Connect i.e.without ever having invoking OnConnectionDownAsync during
                        // a formal client disconnect. To allow authors of CircuitHandlers to have reasonable expectations will pair the connection up with a connection down.
                        await circuitHost.OnConnectionDownAsync(cancellationToken);
                    }

                    await circuitHost.OnConnectionUpAsync(cancellationToken);
                });
            }

            try
            {
                await circuitHandlerTask;
                Log.ReconnectionSucceeded(_logger, circuitHost.CircuitId);
                return circuitHost;
            }
            catch (Exception ex)
            {
                Log.FailedToReconnectToCircuit(_logger, circuitHost.CircuitId, ex);
                await TerminateAsync(circuitId);

                // Return null on failure, because we need to clean up the circuit.
                return null;
            }
        }

        protected virtual (CircuitHost circuitHost, bool previouslyConnected) ConnectCore(CircuitId circuitId, IClientProxy clientProxy, string connectionId)
        {
            if (ConnectedCircuits.TryGetValue(circuitId, out var connectedCircuitHost))
            {
                Log.ConnectingToActiveCircuit(_logger, connectedCircuitHost.CircuitId, connectionId);

                // The host is still active i.e. the server hasn't detected the client disconnect.
                // However the client reconnected establishing a new connection.
                connectedCircuitHost.Client.Transfer(clientProxy, connectionId);
                return (connectedCircuitHost, true);
            }

            if (DisconnectedCircuits.TryGetValue(circuitId.Secret, out DisconnectedCircuitEntry disconnectedEntry))
            {
                Log.ConnectingToDisconnectedCircuit(_logger, disconnectedEntry.CircuitHost.CircuitId, connectionId);

                // The host was in disconnected state. Transfer it to ConnectedCircuits so that it's no longer considered disconnected.
                // First discard the CancellationTokenSource so that the cache entry does not expire.
                DisposeTokenSource(disconnectedEntry);

                DisconnectedCircuits.Remove(circuitId.Secret);
                ConnectedCircuits.TryAdd(circuitId, disconnectedEntry.CircuitHost);

                disconnectedEntry.CircuitHost.Client.Transfer(clientProxy, connectionId);
                return (disconnectedEntry.CircuitHost, false);
            }

            return default;
        }

        protected virtual void OnEntryEvicted(object key, object value, EvictionReason reason, object state)
        {
            switch (reason)
            {
                case EvictionReason.Expired:
                case EvictionReason.TokenExpired:
                case EvictionReason.Capacity:
                    // Kick off the dispose in the background.
                    var disconnectedEntry = (DisconnectedCircuitEntry)value;
                    Log.CircuitEvicted(_logger, disconnectedEntry.CircuitHost.CircuitId, reason);
                    _ = DisposeCircuitEntry(disconnectedEntry);
                    break;

                case EvictionReason.Removed:
                    // The entry was explicitly removed as part of TryGetInactiveCircuit. Nothing to do here.
                    return;

                default:
                    Debug.Fail($"Unexpected {nameof(EvictionReason)} {reason}");
                    break;
            }
        }

        private async Task DisposeCircuitEntry(DisconnectedCircuitEntry entry)
        {
            DisposeTokenSource(entry);

            try
            {
                entry.CircuitHost.UnhandledException -= CircuitHost_UnhandledException;
                await entry.CircuitHost.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log.UnhandledExceptionDisposingCircuitHost(_logger, ex);
            }
        }

        private void DisposeTokenSource(DisconnectedCircuitEntry entry)
        {
            try
            {
                entry.TokenSource.Dispose();
            }
            catch (Exception ex)
            {
                Log.ExceptionDisposingTokenSource(_logger, ex);
            }
        }

        // We don't expect this to throw. User code only runs inside DisposeAsync and that does its own error handling.
        public ValueTask TerminateAsync(CircuitId circuitId)
        {
            CircuitHost circuitHost;
            DisconnectedCircuitEntry entry = default;
            lock (CircuitRegistryLock)
            {
                if (ConnectedCircuits.TryGetValue(circuitId, out circuitHost) || DisconnectedCircuits.TryGetValue(circuitId.Secret, out entry))
                {
                    circuitHost ??= entry.CircuitHost;
                    DisconnectedCircuits.Remove(circuitId.Secret);
                    ConnectedCircuits.TryRemove(circuitId, out _);
                    Log.CircuitDisconnectedPermanently(_logger, circuitHost.CircuitId);
                    circuitHost.Client.SetDisconnected();
                }
            }

            if (circuitHost != null)
            {
                circuitHost.UnhandledException -= CircuitHost_UnhandledException;
                return circuitHost.DisposeAsync();
            }

            return default;
        }

        // We don't need to do anything with the exception here, logging and sending exceptions to the client
        // is done inside the circuit host.
        private async void CircuitHost_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var circuitHost = (CircuitHost)sender;

            try
            {
                // This will dispose the circuit and remove it from the registry.
                await TerminateAsync(circuitHost.CircuitId);
            }
            catch (Exception ex)
            {
                // We don't expect TerminateAsync to throw, but we want exceptions here for completeness.
                Log.CircuitExceptionHandlerFailed(_logger, circuitHost.CircuitId, ex);
            }
        }

        private readonly struct DisconnectedCircuitEntry
        {
            public DisconnectedCircuitEntry(CircuitHost circuitHost, CancellationTokenSource tokenSource)
            {
                CircuitHost = circuitHost;
                TokenSource = tokenSource;
            }

            public CircuitHost CircuitHost { get; }
            public CancellationTokenSource TokenSource { get; }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _exceptionDisposingCircuitHost;
            private static readonly Action<ILogger, string, Exception> _unhandledExceptionDisposingTokenSource;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitReconnectStarted;
            private static readonly Action<ILogger, CircuitId, Exception> _failedToFindCircuit;
            private static readonly Action<ILogger, CircuitId, string, Exception> _connectingToActiveCircuit;
            private static readonly Action<ILogger, CircuitId, string, Exception> _connectingToDisconnectedCircuit;
            private static readonly Action<ILogger, CircuitId, Exception> _failedToReconnectToCircuit;
            private static readonly Action<ILogger, CircuitId, Exception> _reconnectionSucceeded;
            private static readonly Action<ILogger, CircuitId, string, Exception> _circuitDisconnectStarted;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitNotActive;
            private static readonly Action<ILogger, CircuitId, string, Exception> _circuitConnectedToDifferentConnection;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitMarkedDisconnected;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitDisconnectedPermanently;
            private static readonly Action<ILogger, CircuitId, EvictionReason, Exception> _circuitEvicted;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitExceptionHandlerFailed;

            private static class EventIds
            {
                public static readonly EventId ExceptionDisposingCircuit = new EventId(100, "ExceptionDisposingCircuit");
                public static readonly EventId ExceptionDisposingTokenSource = new EventId(101, "ExceptionDisposingTokenSource");
                public static readonly EventId AttemptingToReconnect = new EventId(102, "AttemptingToReconnect");
                public static readonly EventId FailedToFindCircuit = new EventId(104, "FailedToFindCircuit");
                public static readonly EventId ConnectingToActiveCircuit = new EventId(105, "ConnectingToActiveCircuit");
                public static readonly EventId ConnectingToDisconnectedCircuit = new EventId(106, "ConnectingToDisconnectedCircuit");
                public static readonly EventId FailedToReconnectToCircuit = new EventId(107, "FailedToReconnectToCircuit");
                public static readonly EventId CircuitDisconnectStarted = new EventId(108, "CircuitDisconnectStarted");
                public static readonly EventId CircuitNotActive = new EventId(109, "CircuitNotActive");
                public static readonly EventId CircuitConnectedToDifferentConnection = new EventId(110, "CircuitConnectedToDifferentConnection");
                public static readonly EventId CircuitMarkedDisconnected = new EventId(111, "CircuitMarkedDisconnected");
                public static readonly EventId CircuitEvicted = new EventId(112, "CircuitEvicted");
                public static readonly EventId CircuitDisconnectedPermanently = new EventId(113, "CircuitDisconnectedPermanently");
                public static readonly EventId CircuitExceptionHandlerFailed = new EventId(114, "CircuitExceptionHandlerFailed");
            }

            static Log()
            {
                _exceptionDisposingCircuitHost = LoggerMessage.Define<string>(
                    LogLevel.Error,
                    EventIds.ExceptionDisposingCircuit,
                    "Unhandled exception disposing circuit host: {Message}");

                _unhandledExceptionDisposingTokenSource = LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    EventIds.ExceptionDisposingTokenSource,
                    "Exception thrown when disposing token source: {Message}");

                _circuitReconnectStarted = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.AttemptingToReconnect,
                    "Attempting to reconnect to Circuit with secret {CircuitHost}.");

                _failedToFindCircuit = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.FailedToFindCircuit,
                    "Failed to find a matching circuit for circuit secret {CircuitHost}.");

                _connectingToActiveCircuit = LoggerMessage.Define<CircuitId, string>(
                    LogLevel.Debug,
                    EventIds.ConnectingToActiveCircuit,
                    "Transferring active circuit {CircuitId} to connection {ConnectionId}.");

                _connectingToDisconnectedCircuit = LoggerMessage.Define<CircuitId, string>(
                    LogLevel.Debug,
                    EventIds.ConnectingToDisconnectedCircuit,
                    "Transferring disconnected circuit {CircuitId} to connection {ConnectionId}.");

                _failedToReconnectToCircuit = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.FailedToReconnectToCircuit,
                    "Failed to reconnect to a circuit with id {CircuitId}.");

                _reconnectionSucceeded = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.FailedToReconnectToCircuit,
                    "Reconnect to circuit with id {CircuitId} succeeded.");

                _circuitDisconnectStarted = LoggerMessage.Define<CircuitId, string>(
                    LogLevel.Debug,
                    EventIds.CircuitDisconnectStarted,
                    "Attempting to disconnect circuit with id {CircuitId} from connection {ConnectionId}.");

                _circuitNotActive = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.CircuitNotActive,
                    "Failed to disconnect circuit with id {CircuitId}. The circuit is not active.");

                _circuitConnectedToDifferentConnection = LoggerMessage.Define<CircuitId, string>(
                    LogLevel.Debug,
                    EventIds.CircuitConnectedToDifferentConnection,
                    "Failed to disconnect circuit with id {CircuitId}. The circuit is connected to {ConnectionId}.");

                _circuitMarkedDisconnected = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.CircuitMarkedDisconnected,
                    "Circuit with id {CircuitId} is disconnected.");

                _circuitDisconnectedPermanently = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.CircuitDisconnectedPermanently,
                    "Circuit with id {CircuitId} has been removed from the registry for permanent disconnection.");

                _circuitEvicted = LoggerMessage.Define<CircuitId, EvictionReason>(
                    LogLevel.Debug,
                    EventIds.CircuitEvicted,
                    "Circuit with id {CircuitId} evicted due to {EvictionReason}.");

                _circuitExceptionHandlerFailed = LoggerMessage.Define<CircuitId>(
                    LogLevel.Error,
                    EventIds.CircuitExceptionHandlerFailed,
                    "Exception handler for {CircuitId} failed.");
            }

            public static void UnhandledExceptionDisposingCircuitHost(ILogger logger, Exception exception) =>
                _exceptionDisposingCircuitHost(logger, exception.Message, exception);

            public static void ExceptionDisposingTokenSource(ILogger logger, Exception exception) =>
                _unhandledExceptionDisposingTokenSource(logger, exception.Message, exception);

            public static void CircuitConnectStarted(ILogger logger, CircuitId circuitId) =>
                _circuitReconnectStarted(logger, circuitId, null);

            public static void FailedToFindCircuit(ILogger logger, CircuitId circuitId) =>
                _failedToFindCircuit(logger, circuitId, null);

            public static void ConnectingToActiveCircuit(ILogger logger, CircuitId circuitId, string connectionId) =>
                _connectingToActiveCircuit(logger, circuitId, connectionId, null);

            public static void ConnectingToDisconnectedCircuit(ILogger logger, CircuitId circuitId, string connectionId) =>
                _connectingToDisconnectedCircuit(logger, circuitId, connectionId, null);

            public static void FailedToReconnectToCircuit(ILogger logger, CircuitId circuitId, Exception exception = null) =>
                _failedToReconnectToCircuit(logger, circuitId, exception);

            public static void ReconnectionSucceeded(ILogger logger, CircuitId circuitId) =>
                _reconnectionSucceeded(logger, circuitId, null);

            public static void CircuitDisconnectStarted(ILogger logger, CircuitId circuitId, string connectionId) =>
                _circuitDisconnectStarted(logger, circuitId, connectionId, null);

            public static void CircuitNotActive(ILogger logger, CircuitId circuitId) =>
                _circuitNotActive(logger, circuitId, null);

            public static void CircuitConnectedToDifferentConnection(ILogger logger, CircuitId circuitId, string connectionId) =>
                _circuitConnectedToDifferentConnection(logger, circuitId, connectionId, null);

            public static void CircuitMarkedDisconnected(ILogger logger, CircuitId circuitId) =>
                _circuitMarkedDisconnected(logger, circuitId, null);

            public static void CircuitDisconnectedPermanently(ILogger logger, CircuitId circuitId) =>
                _circuitDisconnectedPermanently(logger, circuitId, null);

            public static void CircuitEvicted(ILogger logger, CircuitId circuitId, EvictionReason evictionReason) =>
               _circuitEvicted(logger, circuitId, evictionReason, null);

            public static void CircuitExceptionHandlerFailed(ILogger logger, CircuitId circuitId, Exception exception) =>
                _circuitExceptionHandlerFailed(logger, circuitId, exception);
        }
    }
}
