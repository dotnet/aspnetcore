// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server;

// Some notes about our expectations for error handling:
//
// In general, we need to prevent any client from interacting with a circuit that's in an unpredictable
// state. This means that when a circuit throws an unhandled exception our top priority is to
// unregister and dispose the circuit. This will prevent any new dispatches from the client
// from making it into application code.
//
// As part of this process, we also notify the client (if there is one) of the error, and we
// *expect* a well-behaved client to disconnect. A malicious client can't be expected to disconnect,
// but since we've unregistered the circuit they won't be able to access it anyway. When a call
// comes into any hub method and the circuit has been disassociated, we will abort the connection.
// It's safe to assume that's the result of a race condition or misbehaving client.
//
// Now it's important to remember that we can only abort a connection as part of a hub method call.
// We can dispose a circuit in the background, but we have to deal with a possible race condition
// any time we try to acquire access to the circuit - because it could have gone away in the
// background - outside of the scope of a hub method.
//
// In general we author our Hub methods as async methods, but we fire-and-forget anything that
// needs access to the circuit/application state to unblock the message loop. Using async in our
// Hub methods allows us to ensure message delivery to the client before we abort the connection
// in error cases.
internal sealed partial class ComponentHub : Hub
{
    private static readonly object CircuitKey = new();
    private readonly IServerComponentDeserializer _serverComponentSerializer;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ICircuitFactory _circuitFactory;
    private readonly CircuitIdFactory _circuitIdFactory;
    private readonly CircuitRegistry _circuitRegistry;
    private readonly ICircuitHandleRegistry _circuitHandleRegistry;
    private readonly ILogger _logger;

    public ComponentHub(
        IServerComponentDeserializer serializer,
        IDataProtectionProvider dataProtectionProvider,
        ICircuitFactory circuitFactory,
        CircuitIdFactory circuitIdFactory,
        CircuitRegistry circuitRegistry,
        ICircuitHandleRegistry circuitHandleRegistry,
        ILogger<ComponentHub> logger)
    {
        _serverComponentSerializer = serializer;
        _dataProtectionProvider = dataProtectionProvider;
        _circuitFactory = circuitFactory;
        _circuitIdFactory = circuitIdFactory;
        _circuitRegistry = circuitRegistry;
        _circuitHandleRegistry = circuitHandleRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default endpoint path for incoming connections.
    /// </summary>
    public static PathString DefaultPath { get; } = "/_blazor";

    public override Task OnDisconnectedAsync(Exception exception)
    {
        // If the CircuitHost is gone now this isn't an error. This could happen if the disconnect
        // if the result of well behaving client hanging up after an unhandled exception.
        var circuitHost = _circuitHandleRegistry.GetCircuit(Context.Items, CircuitKey);
        if (circuitHost == null)
        {
            return Task.CompletedTask;
        }

        return _circuitRegistry.DisconnectAsync(circuitHost, Context.ConnectionId);
    }

    public async ValueTask<string> StartCircuit(string baseUri, string uri, string serializedComponentRecords, string applicationState)
    {
        var circuitHost = _circuitHandleRegistry.GetCircuit(Context.Items, CircuitKey);
        if (circuitHost != null)
        {
            // This is an error condition and an attempt to bind multiple circuits to a single connection.
            // We can reject this and terminate the connection.
            Log.CircuitAlreadyInitialized(_logger, circuitHost.CircuitId);
            await NotifyClientError(Clients.Caller, $"The circuit host '{circuitHost.CircuitId}' has already been initialized.");
            Context.Abort();
            return null;
        }

        if (baseUri == null ||
            uri == null ||
            !Uri.TryCreate(baseUri, UriKind.Absolute, out _) ||
            !Uri.TryCreate(uri, UriKind.Absolute, out _))
        {
            // We do some really minimal validation here to prevent obviously wrong data from getting in
            // without duplicating too much logic.
            //
            // This is an error condition attempting to initialize the circuit in a way that would fail.
            // We can reject this and terminate the connection.
            Log.InvalidInputData(_logger);
            await NotifyClientError(Clients.Caller, "The uris provided are invalid.");
            Context.Abort();
            return null;
        }

        if (!_serverComponentSerializer.TryDeserializeComponentDescriptorCollection(serializedComponentRecords, out var components))
        {
            Log.InvalidInputData(_logger);
            await NotifyClientError(Clients.Caller, "The list of component records is not valid.");
            Context.Abort();
            return null;
        }

        try
        {
            var circuitClient = new CircuitClientProxy(Clients.Caller, Context.ConnectionId);
            var store = !string.IsNullOrEmpty(applicationState) ?
                new ProtectedPrerenderComponentApplicationStore(applicationState, _dataProtectionProvider) :
                new ProtectedPrerenderComponentApplicationStore(_dataProtectionProvider);
            var resourceCollection = Context.GetHttpContext().GetEndpoint()?.Metadata.GetMetadata<ResourceAssetCollection>();
            circuitHost = await _circuitFactory.CreateCircuitHostAsync(
                components,
                circuitClient,
                baseUri,
                uri,
                Context.User,
                store,
                resourceCollection);

            // Fire-and-forget the initialization process, because we can't block the
            // SignalR message loop (we'd get a deadlock if any of the initialization
            // logic relied on receiving a subsequent message from SignalR), and it will
            // take care of its own errors anyway.
            _ = circuitHost.InitializeAsync(store, Context.ConnectionAborted);

            // It's safe to *publish* the circuit now because nothing will be able
            // to run inside it until after InitializeAsync completes.
            _circuitRegistry.Register(circuitHost);
            _circuitHandleRegistry.SetCircuit(Context.Items, CircuitKey, circuitHost);

            // Returning the secret here so the client can reconnect.
            //
            // Logging the secret and circuit ID here so we can associate them with just logs (if TRACE level is on).
            Log.CreatedCircuit(_logger, circuitHost.CircuitId, circuitHost.CircuitId.Secret, Context.ConnectionId);
            return circuitHost.CircuitId.Secret;
        }
        catch (Exception ex)
        {
            // If the circuit fails to initialize synchronously we can notify the client immediately
            // and shut down the connection.
            Log.CircuitInitializationFailed(_logger, ex);
            await NotifyClientError(Clients.Caller, "The circuit failed to initialize.");
            Context.Abort();
            return null;
        }
    }

    public async Task UpdateRootComponents(string serializedComponentOperations, string applicationState)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            return;
        }

        if (!_serverComponentSerializer.TryDeserializeRootComponentOperations(
            serializedComponentOperations,
            out var operations))
        {
            // There was an error, so kill the circuit.
            await _circuitRegistry.TerminateAsync(circuitHost.CircuitId);
            await NotifyClientError(Clients.Caller, "The list of component operations is not valid.");
            Context.Abort();

            return;
        }

        var store = !string.IsNullOrEmpty(applicationState) ?
            new ProtectedPrerenderComponentApplicationStore(applicationState, _dataProtectionProvider) :
            new ProtectedPrerenderComponentApplicationStore(_dataProtectionProvider);

        _ = circuitHost.UpdateRootComponents(operations, store, Context.ConnectionAborted);
    }

    public async ValueTask<bool> ConnectCircuit(string circuitIdSecret)
    {
        // TryParseCircuitId will not throw.
        if (!_circuitIdFactory.TryParseCircuitId(circuitIdSecret, out var circuitId))
        {
            // Invalid id.
            Log.InvalidCircuitId(_logger, circuitIdSecret);
            return false;
        }

        // ConnectAsync will not throw.
        var circuitHost = await _circuitRegistry.ConnectAsync(
            circuitId,
            Clients.Caller,
            Context.ConnectionId,
            Context.ConnectionAborted);
        if (circuitHost != null)
        {
            _circuitHandleRegistry.SetCircuit(Context.Items, CircuitKey, circuitHost);
            circuitHost.SetCircuitUser(Context.User);
            circuitHost.SendPendingBatches();
            return true;
        }

        // If we get here the circuit does not exist anymore. This is something that's valid for a client to
        // recover from, and the client is not holding any resources right now other than the connection.
        return false;
    }

    public async ValueTask BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            return;
        }

        _ = circuitHost.BeginInvokeDotNetFromJS(callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson);
    }

    public async ValueTask EndInvokeJSFromDotNet(long asyncHandle, bool succeeded, string arguments)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            return;
        }

        _ = circuitHost.EndInvokeJSFromDotNet(asyncHandle, succeeded, arguments);
    }

    public async ValueTask ReceiveByteArray(int id, byte[] data)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            return;
        }

        _ = circuitHost.ReceiveByteArray(id, data);
    }

    public async ValueTask<bool> ReceiveJSDataChunk(long streamId, long chunkId, byte[] chunk, string error)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            return false;
        }

        // Note: this await will block the circuit. This is intentional.
        // The call into the circuitHost.ReceiveJSDataChunk will block regardless as we call into Renderer.Dispatcher.InvokeAsync
        // which ensures we're running on the main circuit thread so that the server/client remain in the same
        // synchronization context. Additionally, we're utilizing the return value as a heartbeat for the transfer
        // process, and without it would likely need to setup a separate endpoint to handle that functionality.
        return await circuitHost.ReceiveJSDataChunk(streamId, chunkId, chunk, error);
    }

    public async IAsyncEnumerable<ArraySegment<byte>> SendDotNetStreamToJS(long streamId)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            yield break;
        }

        var dotNetStreamReference = await circuitHost.TryClaimPendingStream(streamId);
        if (dotNetStreamReference == default)
        {
            yield break;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(32 * 1024);

        try
        {
            int bytesRead;
            while ((bytesRead = await circuitHost.SendDotNetStreamAsync(dotNetStreamReference, streamId, buffer)) > 0)
            {
                yield return new ArraySegment<byte>(buffer, 0, bytesRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);

            if (!dotNetStreamReference.LeaveOpen)
            {
                dotNetStreamReference.Stream?.Dispose();
            }
        }
    }

    public async ValueTask OnRenderCompleted(long renderId, string errorMessageOrNull)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            return;
        }

        Log.ReceivedConfirmationForBatch(_logger, renderId);
        _ = circuitHost.OnRenderCompletedAsync(renderId, errorMessageOrNull);
    }

    public async ValueTask OnLocationChanged(string uri, string? state, bool intercepted)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            return;
        }

        _ = circuitHost.OnLocationChangedAsync(uri, state, intercepted);
    }

    public async ValueTask OnLocationChanging(int callId, string uri, string? state, bool intercepted)
    {
        var circuitHost = await GetActiveCircuitAsync();
        if (circuitHost == null)
        {
            return;
        }

        _ = circuitHost.OnLocationChangingAsync(callId, uri, state, intercepted);
    }

    // We store the CircuitHost through a *handle* here because Context.Items is tied to the lifetime
    // of the connection. It's possible that a misbehaving client could cause disposal of a CircuitHost
    // but keep a connection open indefinitely, preventing GC of the Circuit and related application state.
    // Using a handle allows the CircuitHost to clear this reference in the background.
    //
    // See comment on error handling on the class definition.
    private async ValueTask<CircuitHost> GetActiveCircuitAsync([CallerMemberName] string callSite = "")
    {
        var handle = _circuitHandleRegistry.GetCircuitHandle(Context.Items, CircuitKey);
        var circuitHost = handle?.CircuitHost;
        if (handle != null && circuitHost == null)
        {
            // This can occur when a circuit host does not exist anymore due to an unhandled exception.
            // We can reject this and terminate the connection.
            Log.CircuitHostShutdown(_logger, callSite);
            await NotifyClientError(Clients.Caller, "Circuit has been shut down due to error.");
            Context.Abort();
            return null;
        }
        else if (circuitHost == null)
        {
            // This can occur when a circuit host does not exist anymore due to an unhandled exception.
            // We can reject this and terminate the connection.
            Log.CircuitHostNotInitialized(_logger, callSite);
            await NotifyClientError(Clients.Caller, "Circuit not initialized.");
            Context.Abort();
            return null;
        }

        return circuitHost;
    }

    private static Task NotifyClientError(IClientProxy client, string error) => client.SendAsync("JS.Error", error);

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Received confirmation for batch {BatchId}", EventName = "ReceivedConfirmationForBatch")]
        public static partial void ReceivedConfirmationForBatch(ILogger logger, long batchId);

        [LoggerMessage(2, LogLevel.Debug, "The circuit host '{CircuitId}' has already been initialized", EventName = "CircuitAlreadyInitialized")]
        public static partial void CircuitAlreadyInitialized(ILogger logger, CircuitId circuitId);

        [LoggerMessage(3, LogLevel.Debug, "Call to '{CallSite}' received before the circuit host initialization", EventName = "CircuitHostNotInitialized")]
        public static partial void CircuitHostNotInitialized(ILogger logger, [CallerMemberName] string callSite = "");

        [LoggerMessage(4, LogLevel.Debug, "Call to '{CallSite}' received after the circuit was shut down", EventName = "CircuitHostShutdown")]
        public static partial void CircuitHostShutdown(ILogger logger, [CallerMemberName] string callSite = "");

        [LoggerMessage(5, LogLevel.Debug, "Call to '{CallSite}' received invalid input data", EventName = "InvalidInputData")]
        public static partial void InvalidInputData(ILogger logger, [CallerMemberName] string callSite = "");

        [LoggerMessage(6, LogLevel.Debug, "Circuit initialization failed", EventName = "CircuitInitializationFailed")]
        public static partial void CircuitInitializationFailed(ILogger logger, Exception exception);

        [LoggerMessage(7, LogLevel.Debug, "Created circuit '{CircuitId}' with secret '{CircuitIdSecret}' for '{ConnectionId}'", EventName = "CreatedCircuit")]
        private static partial void CreatedCircuitCore(ILogger logger, CircuitId circuitId, string circuitIdSecret, string connectionId);

        public static void CreatedCircuit(ILogger logger, CircuitId circuitId, string circuitSecret, string connectionId)
        {
            // Redact the secret unless tracing is on.
            if (!logger.IsEnabled(LogLevel.Trace))
            {
                circuitSecret = "(redacted)";
            }

            CreatedCircuitCore(logger, circuitId, circuitSecret, connectionId);
        }

        [LoggerMessage(8, LogLevel.Debug, "ConnectAsync received an invalid circuit id '{CircuitIdSecret}'", EventName = "InvalidCircuitId")]
        private static partial void InvalidCircuitIdCore(ILogger logger, string circuitIdSecret);

        public static void InvalidCircuitId(ILogger logger, string circuitSecret)
        {
            // Redact the secret unless tracing is on.
            if (!logger.IsEnabled(LogLevel.Trace))
            {
                circuitSecret = "(redacted)";
            }

            InvalidCircuitIdCore(logger, circuitSecret);
        }
    }
}
