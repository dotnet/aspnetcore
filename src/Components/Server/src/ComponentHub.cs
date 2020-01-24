// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
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
    internal sealed class ComponentHub : Hub
    {
        private static readonly object CircuitKey = new object();
        private readonly ServerComponentDeserializer _serverComponentSerializer;
        private readonly CircuitFactory _circuitFactory;
        private readonly CircuitIdFactory _circuitIdFactory;
        private readonly CircuitRegistry _circuitRegistry;
        private readonly ILogger _logger;

        public ComponentHub(
            ServerComponentDeserializer serializer,
            CircuitFactory circuitFactory,
            CircuitIdFactory circuitIdFactory,
            CircuitRegistry circuitRegistry,
            ILogger<ComponentHub> logger)
        {
            _serverComponentSerializer = serializer;
            _circuitFactory = circuitFactory;
            _circuitIdFactory = circuitIdFactory;
            _circuitRegistry = circuitRegistry;
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
            var circuitHost = GetCircuit();
            if (circuitHost == null)
            {
                return Task.CompletedTask;
            }

            return _circuitRegistry.DisconnectAsync(circuitHost, Context.ConnectionId);
        }

        public async ValueTask<string> StartCircuit(string baseUri, string uri, string serializedComponentRecords)
        {
            var circuitHost = GetCircuit();
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
                !Uri.IsWellFormedUriString(baseUri, UriKind.Absolute) ||
                !Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                // We do some really minimal validation here to prevent obviously wrong data from getting in
                // without duplicating too much logic.
                //
                // This is an error condition attempting to initialize the circuit in a way that would fail.
                // We can reject this and terminate the connection.
                Log.InvalidInputData(_logger);
                await NotifyClientError(Clients.Caller, $"The uris provided are invalid.");
                Context.Abort();
                return null;
            }

            if (!_serverComponentSerializer.TryDeserializeComponentDescriptorCollection(serializedComponentRecords, out var components))
            {
                Log.InvalidInputData(_logger);
                await NotifyClientError(Clients.Caller, $"The list of component records is not valid.");
                Context.Abort();
                return null;
            }

            try
            {
                var circuitClient = new CircuitClientProxy(Clients.Caller, Context.ConnectionId);
                circuitHost = _circuitFactory.CreateCircuitHost(
                    components,
                    circuitClient,
                    baseUri,
                    uri,
                    Context.User);

                // Fire-and-forget the initialization process, because we can't block the
                // SignalR message loop (we'd get a deadlock if any of the initialization
                // logic relied on receiving a subsequent message from SignalR), and it will
                // take care of its own errors anyway.
                _ = circuitHost.InitializeAsync(Context.ConnectionAborted);

                // It's safe to *publish* the circuit now because nothing will be able
                // to run inside it until after InitializeAsync completes.
                _circuitRegistry.Register(circuitHost);
                SetCircuit(circuitHost);

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
                SetCircuit(circuitHost);
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

        public async ValueTask DispatchBrowserEvent(string eventDescriptor, string eventArgs)
        {
            var circuitHost = await GetActiveCircuitAsync();
            if (circuitHost == null)
            {
                return;
            }

            _ = circuitHost.DispatchEvent(eventDescriptor, eventArgs);
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

        public async ValueTask OnLocationChanged(string uri, bool intercepted)
        {
            var circuitHost = await GetActiveCircuitAsync();
            if (circuitHost == null)
            {
                return;
            }

            _ = circuitHost.OnLocationChangedAsync(uri, intercepted);
        }

        // We store the CircuitHost through a *handle* here because Context.Items is tied to the lifetime
        // of the connection. It's possible that a misbehaving client could cause disposal of a CircuitHost
        // but keep a connection open indefinitely, preventing GC of the Circuit and related application state.
        // Using a handle allows the CircuitHost to clear this reference in the background.
        //
        // See comment on error handling on the class definition.
        private async ValueTask<CircuitHost> GetActiveCircuitAsync([CallerMemberName] string callSite = "")
        {
            var handle = (CircuitHandle)Context.Items[CircuitKey];
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

        private CircuitHost GetCircuit()
        {
            return ((CircuitHandle)Context.Items[CircuitKey])?.CircuitHost;
        }

        private void SetCircuit(CircuitHost circuitHost)
        {
            Context.Items[CircuitKey] = circuitHost?.Handle;
        }

        private static Task NotifyClientError(IClientProxy client, string error) => client.SendAsync("JS.Error", error);

        private static class Log
        {
            private static readonly Action<ILogger, long, Exception> _receivedConfirmationForBatch =
                LoggerMessage.Define<long>(LogLevel.Debug, new EventId(1, "ReceivedConfirmationForBatch"), "Received confirmation for batch {BatchId}");

            private static readonly Action<ILogger, CircuitId, Exception> _circuitAlreadyInitialized =
                LoggerMessage.Define<CircuitId>(LogLevel.Debug, new EventId(2, "CircuitAlreadyInitialized"), "The circuit host '{CircuitId}' has already been initialized");

            private static readonly Action<ILogger, string, Exception> _circuitHostNotInitialized =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "CircuitHostNotInitialized"), "Call to '{CallSite}' received before the circuit host initialization");

            private static readonly Action<ILogger, string, Exception> _circuitHostShutdown =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "CircuitHostShutdown"), "Call to '{CallSite}' received after the circuit was shut down");

            private static readonly Action<ILogger, string, Exception> _invalidInputData =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, "InvalidInputData"), "Call to '{CallSite}' received invalid input data");

            private static readonly Action<ILogger, Exception> _circuitInitializationFailed =
                LoggerMessage.Define(LogLevel.Debug, new EventId(6, "CircuitInitializationFailed"), "Circuit initialization failed");

            private static readonly Action<ILogger, CircuitId, string, string, Exception> _createdCircuit =
                LoggerMessage.Define<CircuitId, string, string>(LogLevel.Debug, new EventId(7, "CreatedCircuit"), "Created circuit '{CircuitId}' with secret '{CircuitIdSecret}' for '{ConnectionId}'");

            private static readonly Action<ILogger, string, Exception> _invalidCircuitId =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(8, "InvalidCircuitId"), "ConnectAsync received an invalid circuit id '{CircuitIdSecret}'");

            public static void ReceivedConfirmationForBatch(ILogger logger, long batchId) => _receivedConfirmationForBatch(logger, batchId, null);

            public static void CircuitAlreadyInitialized(ILogger logger, CircuitId circuitId) => _circuitAlreadyInitialized(logger, circuitId, null);

            public static void CircuitHostNotInitialized(ILogger logger, [CallerMemberName] string callSite = "") => _circuitHostNotInitialized(logger, callSite, null);

            public static void CircuitHostShutdown(ILogger logger, [CallerMemberName] string callSite = "") => _circuitHostShutdown(logger, callSite, null);

            public static void InvalidInputData(ILogger logger, [CallerMemberName] string callSite = "") => _invalidInputData(logger, callSite, null);

            public static void CircuitInitializationFailed(ILogger logger, Exception exception) => _circuitInitializationFailed(logger, exception);

            public static void CreatedCircuit(ILogger logger, CircuitId circuitId, string circuitSecret, string connectionId)
            {
                // Redact the secret unless tracing is on.
                if (!logger.IsEnabled(LogLevel.Trace))
                {
                    circuitSecret = "(redacted)";
                }

                _createdCircuit(logger, circuitId, circuitSecret, connectionId, null);
            }

            public static void InvalidCircuitId(ILogger logger, string circuitSecret)
            {
                // Redact the secret unless tracing is on.
                if (!logger.IsEnabled(LogLevel.Trace))
                {
                    circuitSecret = "(redacted)";
                }

                _invalidCircuitId(logger, circuitSecret, null);
            }
        }
    }
}
