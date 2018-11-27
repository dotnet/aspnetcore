// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    /// <summary>
    /// A SignalR hub that accepts connections to a Server-Side Blazor app.
    /// </summary>
    public sealed class BlazorHub : Hub
    {
        private static readonly object CircuitKey = new object();
        private readonly CircuitFactory _circuitFactory;
        private readonly ILogger _logger;

        /// <summary>
        /// Intended for framework use only. Applications should not instantiate
        /// this class directly.
        /// </summary>
        public BlazorHub(
            ILogger<BlazorHub> logger,
            IServiceProvider services)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _circuitFactory = services.GetRequiredService<CircuitFactory>();
        }

        /// <summary>
        /// Gets the default endpoint path for incoming connections.
        /// </summary>
        public static PathString DefaultPath => "/_blazor";

        private CircuitHost CircuitHost
        {
            get => (CircuitHost)Context.Items[CircuitKey];
            set => Context.Items[CircuitKey] = value;
        }

        /// <summary>
        /// Intended for framework use only. Applications should not call this method directly.
        /// </summary>
        public override Task OnDisconnectedAsync(Exception exception)
        {
            CircuitHost.Dispose();
            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Intended for framework use only. Applications should not call this method directly.
        /// </summary>
        public async Task StartCircuit(string uriAbsolute, string baseUriAbsolute)
        {
            var circuitHost = _circuitFactory.CreateCircuitHost(Context.GetHttpContext(), Clients.Caller);
            circuitHost.UnhandledException += CircuitHost_UnhandledException;

            var uriHelper = (RemoteUriHelper)circuitHost.Services.GetRequiredService<IUriHelper>();
            uriHelper.Initialize(uriAbsolute, baseUriAbsolute);

            // If initialization fails, this will throw. The caller will explode if they
            // try to call into any interop API.
            await circuitHost.InitializeAsync();
            CircuitHost = circuitHost;
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
            EnsureCircuitHost().Renderer.OnRenderCompleted(renderId, errorMessageOrNull);
        }

        private async void CircuitHost_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var circuitHost = (CircuitHost)sender;
            try
            {
                _logger.LogWarning((Exception)e.ExceptionObject, "Unhandled Server-Side exception");
                await circuitHost.Client.SendAsync("JS.Error", e.ExceptionObject);

                // We generally can't abort the connection here since this is an async
                // callback. The Hub has already been torn down. We'll rely on the
                // client to abort the connection if we successfully transmit an error.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transmit exception to client");
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
    }
}
