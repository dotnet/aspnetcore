// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// Knowing when a client disconnected is not an exact science. There's a fair possiblity that a client may reconnect before the server realizes.
    /// Consequently, we have to account for reconnects and disconnects occuring simultaneously as well as appearing out of order.
    /// To manage this, we use a critical section to manage all state transitions.
    /// </remarks>
    internal class CircuitRegistry
    {
        private readonly object CircuitRegistryLock = new object();
        private readonly CircuitOptions _options;
        private readonly ILogger _logger;
        private readonly PostEvictionCallbackRegistration _postEvictionCallback;

        public CircuitRegistry(
            IOptions<CircuitOptions> options,
            ILogger<CircuitRegistry> logger)
        {
            _options = options.Value;
            _logger = logger;

            ConnectedCircuits = new ConcurrentDictionary<string, CircuitHost>(StringComparer.Ordinal);

            DisconnectedCircuits = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = _options.MaxRetainedDisconnectedCircuits,
            });

            _postEvictionCallback = new PostEvictionCallbackRegistration
            {
                EvictionCallback = OnEntryEvicted,
            };
        }

        internal ConcurrentDictionary<string, CircuitHost> ConnectedCircuits { get; }

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
        }

        public virtual Task DisconnectAsync(CircuitHost circuitHost, string connectionId)
        {
            Task circuitHandlerTask;
            lock (CircuitRegistryLock)
            {
                if (DisconnectCore(circuitHost, connectionId))
                {
                    circuitHandlerTask = circuitHost.Dispatcher.InvokeAsync(() => circuitHost.OnConnectionDownAsync(default));
                }
                else
                {
                    // DisconnectCore may fail to disconnect the circuit if it was previously marked inactive or
                    // has been transfered to a new connection. Do not invoke the circuit handlers in this instance.

                    // We have to do in this instance.
                    return Task.CompletedTask;
                }
            }

            return circuitHandlerTask;
        }

        protected virtual bool DisconnectCore(CircuitHost circuitHost, string connectionId)
        {
            if (!ConnectedCircuits.TryGetValue(circuitHost.CircuitId, out circuitHost))
            {
                // Guard: The circuit might already have been marked as inactive.
                return false;
            }

            if (!string.Equals(circuitHost.Client.ConnectionId, connectionId, StringComparison.Ordinal))
            {
                // The circuit is associated with a different connection. One way this could happen is when
                // the client reconnects with a new connection before the OnDisconnect for the older
                // connection is executed. Do nothing
                return false;
            }

            var result = ConnectedCircuits.TryRemove(circuitHost.CircuitId, out circuitHost);
            Debug.Assert(result, "This operation operates inside of a lock. We expect the previously inspected value to be still here.");

            circuitHost.Client.SetDisconnected();
            var entryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(_options.DisconnectedCircuitRetentionPeriod),
                Size = 1,
                PostEvictionCallbacks = { _postEvictionCallback },
            };

            DisconnectedCircuits.Set(circuitHost.CircuitId, circuitHost, entryOptions);
            return true;
        }

        public virtual async Task<CircuitHost> ConnectAsync(string circuitId, IClientProxy clientProxy, string connectionId, CancellationToken cancellationToken)
        {
            CircuitHost circuitHost;
            bool previouslyConnected;

            Task circuitHandlerTask;

            lock (CircuitRegistryLock)
            {
                // Transition the host from disconnected to connected if it's available. In this critical section, we return
                // an existing host if it's currently considered connected or transition a disconnected host to connected.
                // Transfering also wires up the client to the new set.
                (circuitHost, previouslyConnected) = ConnectCore(circuitId, clientProxy, connectionId);

                if (circuitHost == null)
                {
                    // Failed to connect. Nothing to do here.
                    return null;
                }

                // CircuitHandler events do not need to be executed inside the critical section, however we
                // a) do not want concurrent execution of handler events i.e. a  OnConnectionDownAsync occuring in tandem with a OnConnectionUpAsync for a single circuit.
                // b) out of order connection-up \ connection-down events e.g. a client that disconnects as soon it finishes reconnecting.

                // Dispatch the circuit handlers inside the sync context to ensure the order of execution. CircuitHost executes circuit handlers inside of
                // 
                circuitHandlerTask = circuitHost.Dispatcher.InvokeAsync(async () =>
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

            await circuitHandlerTask;

            return circuitHost;
        }

        protected virtual (CircuitHost circuitHost, bool previouslyConnected) ConnectCore(string circuitId, IClientProxy clientProxy, string connectionId)
        {
            if (ConnectedCircuits.TryGetValue(circuitId, out var circuitHost))
            {
                // The host is still active i.e. the server hasn't detected the client disconnect.
                // However the client reconnected establishing a new connection.
                circuitHost.Client.Transfer(clientProxy, connectionId);
                return (circuitHost, true);
            }

            if (DisconnectedCircuits.TryGetValue(circuitId, out circuitHost))
            {
                // The host was in disconnected state. Transfer it to ConnectedCircuits so that it's no longer considered disconnected.
                DisconnectedCircuits.Remove(circuitId);
                ConnectedCircuits.TryAdd(circuitId, circuitHost);

                circuitHost.Client.Transfer(clientProxy, connectionId);

                return (circuitHost, false);
            }

            return default;
        }

        private void OnEntryEvicted(object key, object value, EvictionReason reason, object state)
        {
            switch (reason)
            {
                case EvictionReason.Expired:
                case EvictionReason.Capacity:
                    // Kick off the dispose in the background.
                    _ = DisposeCircuitHost((CircuitHost)value);
                    break;

                case EvictionReason.Removed:
                    // The entry was explicitly removed as part of TryGetInactiveCircuit. Nothing to do here.
                    return;

                default:
                    Debug.Fail($"Unexpected {nameof(EvictionReason)} {reason}");
                    break;
            }
        }

        private async Task DisposeCircuitHost(CircuitHost circuitHost)
        {
            try
            {
                await circuitHost.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log.UnhandledExceptionDisposingCircuitHost(_logger, ex);
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _unhandledExceptionDisposingCircuitHost;

            private static class EventIds
            {
                public static readonly EventId ExceptionDisposingCircuit = new EventId(100, "ExceptionDisposingCircuit");
            }

            static Log()
            {
                _unhandledExceptionDisposingCircuitHost = LoggerMessage.Define<string>(
                    LogLevel.Error,
                    EventIds.ExceptionDisposingCircuit,
                    "Unhandled exception disposing circuit host: {Message}");
            }

            public static void UnhandledExceptionDisposingCircuitHost(ILogger logger, Exception exception)
            {
                _unhandledExceptionDisposingCircuitHost(
                    logger,
                    exception.Message,
                    exception);
            }
        }
    }
}
