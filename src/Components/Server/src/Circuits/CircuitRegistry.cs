// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

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
#pragma warning disable CA1852 // Seal internal types
internal partial class CircuitRegistry
#pragma warning restore CA1852 // Seal internal types
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

    private static partial class Log
    {
        [LoggerMessage(100, LogLevel.Error, "Unhandled exception disposing circuit host: {Message}", EventName = "ExceptionDisposingCircuit")]
        private static partial void UnhandledExceptionDisposingCircuitHost(ILogger logger, string message, Exception exception);

        public static void UnhandledExceptionDisposingCircuitHost(ILogger logger, Exception exception)
            => UnhandledExceptionDisposingCircuitHost(logger, exception.Message, exception);

        [LoggerMessage(101, LogLevel.Debug, "Exception thrown when disposing token source: {Message}", EventName = "ExceptionDisposingTokenSource")]
        private static partial void ExceptionDisposingTokenSource(ILogger logger, string message, Exception exception);

        public static void ExceptionDisposingTokenSource(ILogger logger, Exception exception)
            => ExceptionDisposingTokenSource(logger, exception.Message, exception);

        [LoggerMessage(102, LogLevel.Debug, "Attempting to reconnect to Circuit with secret {CircuitHost}.", EventName = "AttemptingToReconnect")]
        public static partial void CircuitConnectStarted(ILogger logger, CircuitId circuitHost);

        [LoggerMessage(104, LogLevel.Debug, "Failed to find a matching circuit for circuit secret {CircuitHost}.", EventName = "FailedToFindCircuit")]
        public static partial void FailedToFindCircuit(ILogger logger, CircuitId circuitHost);

        [LoggerMessage(105, LogLevel.Debug, "Transferring active circuit {CircuitId} to connection {ConnectionId}.", EventName = "ConnectingToActiveCircuit")]
        public static partial void ConnectingToActiveCircuit(ILogger logger, CircuitId circuitId, string connectionId);

        [LoggerMessage(106, LogLevel.Debug, "Transferring disconnected circuit {CircuitId} to connection {ConnectionId}.", EventName = "ConnectingToDisconnectedCircuit")]
        public static partial void ConnectingToDisconnectedCircuit(ILogger logger, CircuitId circuitId, string connectionId);

        [LoggerMessage(107, LogLevel.Debug, "Failed to reconnect to a circuit with id {CircuitId}.", EventName = "FailedToReconnectToCircuit")]
        public static partial void FailedToReconnectToCircuit(ILogger logger, CircuitId circuitId, Exception exception = null);

        [LoggerMessage(108, LogLevel.Debug, "Attempting to disconnect circuit with id {CircuitId} from connection {ConnectionId}.", EventName = "CircuitDisconnectStarted")]
        public static partial void CircuitDisconnectStarted(ILogger logger, CircuitId circuitId, string connectionId);

        [LoggerMessage(109, LogLevel.Debug, "Failed to disconnect circuit with id {CircuitId}. The circuit is not active.", EventName = "CircuitNotActive")]
        public static partial void CircuitNotActive(ILogger logger, CircuitId circuitId);

        [LoggerMessage(110, LogLevel.Debug, "Failed to disconnect circuit with id {CircuitId}. The circuit is connected to {ConnectionId}.", EventName = "CircuitConnectedToDifferentConnection")]
        public static partial void CircuitConnectedToDifferentConnection(ILogger logger, CircuitId circuitId, string connectionId);

        [LoggerMessage(111, LogLevel.Debug, "Circuit with id {CircuitId} is disconnected.", EventName = "CircuitMarkedDisconnected")]
        public static partial void CircuitMarkedDisconnected(ILogger logger, CircuitId circuitId);

        [LoggerMessage(112, LogLevel.Debug, "Circuit with id {CircuitId} evicted due to {EvictionReason}.", EventName = "CircuitEvicted")]
        public static partial void CircuitEvicted(ILogger logger, CircuitId circuitId, EvictionReason evictionReason);

        [LoggerMessage(113, LogLevel.Debug, "Circuit with id {CircuitId} has been removed from the registry for permanent disconnection.", EventName = "CircuitDisconnectedPermanently")]
        public static partial void CircuitDisconnectedPermanently(ILogger logger, CircuitId circuitId);

        [LoggerMessage(114, LogLevel.Error, "Exception handler for {CircuitId} failed.", EventName = "CircuitExceptionHandlerFailed")]
        public static partial void CircuitExceptionHandlerFailed(ILogger logger, CircuitId circuitId, Exception exception);

        [LoggerMessage(115, LogLevel.Debug, "Reconnect to circuit with id {CircuitId} succeeded.", EventName = "ReconnectionSucceeded")]
        public static partial void ReconnectionSucceeded(ILogger logger, CircuitId circuitId);
    }
}
