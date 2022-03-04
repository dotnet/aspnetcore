// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    /// <summary>
    /// A SignalR hub that accepts connections to an ASP.NET Core Components application.
    /// </summary>
    internal sealed class ComponentHub : Hub
    {
        private static readonly object CircuitKey = new object();
        private readonly CircuitFactory _circuitFactory;
        private readonly CircuitRegistry _circuitRegistry;
        private readonly ILogger _logger;

        /// <summary>
        /// Intended for framework use only. Applications should not instantiate
        /// this class directly.
        /// </summary>
        public ComponentHub(IServiceProvider services, ILogger<ComponentHub> logger)
        {
            _circuitFactory = services.GetRequiredService<CircuitFactory>();
            _circuitRegistry = services.GetRequiredService<CircuitRegistry>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the default endpoint path for incoming connections.
        /// </summary>
        public static PathString DefaultPath { get; } = "/_blazor";

        /// <summary>
        /// For unit testing only.
        /// </summary>
        internal CircuitHost CircuitHost
        {
            get => (CircuitHost)Context.Items[CircuitKey];
            private set => Context.Items[CircuitKey] = value;
        }

        /// <summary>
        /// Intended for framework use only. Applications should not call this method directly.
        /// </summary>
        public override Task OnDisconnectedAsync(Exception exception)
        {
            var circuitHost = CircuitHost;
            if (circuitHost == null)
            {
                return Task.CompletedTask;
            }

            CircuitHost = null;
            return _circuitRegistry.DisconnectAsync(circuitHost, Context.ConnectionId);
        }

        /// <summary>
        /// Intended for framework use only. Applications should not call this method directly.
        /// </summary>
        public string StartCircuit(string uriAbsolute, string baseUriAbsolute)
        {
            var circuitClient = new CircuitClientProxy(Clients.Caller, Context.ConnectionId);
            if (DefaultCircuitFactory.ResolveComponentMetadata(Context.GetHttpContext(), circuitClient).Count == 0)
            {
                var endpointFeature = Context.GetHttpContext().Features.Get<IEndpointFeature>();
                var endpoint = endpointFeature?.Endpoint;

                Log.NoComponentsRegisteredInEndpoint(_logger, endpoint.DisplayName);

                // No components preregistered so return. This is totally normal if the components were prerendered.
                return null;
            }

            var circuitHost = _circuitFactory.CreateCircuitHost(
                Context.GetHttpContext(),
                circuitClient,
                uriAbsolute,
                baseUriAbsolute);

            circuitHost.UnhandledException += CircuitHost_UnhandledException;

            // Fire-and-forget the initialization process, because we can't block the
            // SignalR message loop (we'd get a deadlock if any of the initialization
            // logic relied on receiving a subsequent message from SignalR), and it will
            // take care of its own errors anyway.
            _ = circuitHost.InitializeAsync(Context.ConnectionAborted);

            _circuitRegistry.Register(circuitHost);

            CircuitHost = circuitHost;

            return circuitHost.CircuitId;
        }

        /// <summary>
        /// Intended for framework use only. Applications should not call this method directly.
        /// </summary>
        public async Task<bool> ConnectCircuit(string circuitId)
        {
            var circuitHost = await _circuitRegistry.ConnectAsync(circuitId, Clients.Caller, Context.ConnectionId, Context.ConnectionAborted);
            if (circuitHost != null)
            {
                CircuitHost = circuitHost;

                circuitHost.InitializeCircuitAfterPrerender(CircuitHost_UnhandledException);
                circuitHost.SendPendingBatches();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Intended for framework use only. Applications should not call this method directly.
        /// </summary>
        public void BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            EnsureCircuitHost().BeginInvokeDotNetFromJS(callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson);
        }

        /// <summary>
        /// Intended for framework use only. Applications should not call this method directly.
        /// </summary>
        public void OnRenderCompleted(long renderId, string errorMessageOrNull)
        {
            Log.ReceivedConfirmationForBatch(_logger, renderId);
            EnsureCircuitHost().Renderer.OnRenderCompleted(renderId, errorMessageOrNull);
        }

        private async void CircuitHost_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var circuitHost = (CircuitHost)sender;
            var circuitId = circuitHost?.CircuitId;

            try
            {
                Log.UnhandledExceptionInCircuit(_logger, circuitId, (Exception)e.ExceptionObject);
                await circuitHost.Client.SendAsync("JS.Error", e.ExceptionObject);

                // We generally can't abort the connection here since this is an async
                // callback. The Hub has already been torn down. We'll rely on the
                // client to abort the connection if we successfully transmit an error.
            }
            catch (Exception ex)
            {
                Log.FailedToTransmitException(_logger, circuitId, ex);
            }
        }

        private CircuitHost EnsureCircuitHost()
        {
            var circuitHost = CircuitHost;
            if (circuitHost == null)
            {
                var message = $"The {nameof(CircuitHost)} is null. This is due to an exception thrown during initialization.";
                throw new InvalidOperationException(message);
            }

            return circuitHost;
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _noComponentsRegisteredInEndpoint =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "NoComponentsRegisteredInEndpoint"), "No components registered in the current endpoint '{Endpoint}'");

            private static readonly Action<ILogger, long, Exception> _receivedConfirmationForBatch =
                LoggerMessage.Define<long>(LogLevel.Debug, new EventId(2, "ReceivedConfirmationForBatch"), "Received confirmation for batch {BatchId}");

            private static readonly Action<ILogger, string, Exception> _unhandledExceptionInCircuit =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3, "UnhandledExceptionInCircuit"), "Unhandled exception in circuit {CircuitId}");

            private static readonly Action<ILogger, string, Exception> _failedToTransmitException =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "FailedToTransmitException"), "Failed to transmit exception to client in circuit {CircuitId}");

            public static void NoComponentsRegisteredInEndpoint(ILogger logger, string endpointDisplayName)
            {
                _noComponentsRegisteredInEndpoint(logger, endpointDisplayName, null);
            }

            public static void ReceivedConfirmationForBatch(ILogger logger, long batchId)
            {
                _receivedConfirmationForBatch(logger, batchId, null);
            }

            public static void UnhandledExceptionInCircuit(ILogger logger, string circuitId, Exception exception)
            {
                _unhandledExceptionInCircuit(logger, circuitId, exception);
            }

            public static void FailedToTransmitException(ILogger logger, string circuitId, Exception transmissionException)
            {
                _failedToTransmitException(logger, circuitId, transmissionException);
            }
        }
    }
}
