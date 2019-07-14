// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class DefaultCircuitFactory : CircuitFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly CircuitIdFactory _circuitIdFactory;

        public DefaultCircuitFactory(
            IServiceScopeFactory scopeFactory,
            ILoggerFactory loggerFactory,
            CircuitIdFactory circuitIdFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<CircuitFactory>();
            _circuitIdFactory = circuitIdFactory ?? throw new ArgumentNullException(nameof(circuitIdFactory));
        }

        public override CircuitHost CreateCircuitHost(
            HttpContext httpContext,
            CircuitClientProxy client,
            string uriAbsolute,
            string baseUriAbsolute)
        {
            var components = ResolveComponentMetadata(httpContext, client);

            var scope = _scopeFactory.CreateScope();
            var encoder = scope.ServiceProvider.GetRequiredService<HtmlEncoder>();
            var jsRuntime = (RemoteJSRuntime)scope.ServiceProvider.GetRequiredService<IJSRuntime>();
            var componentContext = (RemoteComponentContext)scope.ServiceProvider.GetRequiredService<IComponentContext>();
            jsRuntime.Initialize(client);
            componentContext.Initialize(client);

            // You can replace the AuthenticationStateProvider with a custom one, but in that case initialization is up to you
            var authenticationStateProvider = scope.ServiceProvider.GetService<AuthenticationStateProvider>();
            (authenticationStateProvider as FixedAuthenticationStateProvider)?.Initialize(httpContext.User);

            var uriHelper = (RemoteUriHelper)scope.ServiceProvider.GetRequiredService<IUriHelper>();
            var navigationInterception = (RemoteNavigationInterception)scope.ServiceProvider.GetRequiredService<INavigationInterception>();
            if (client.Connected)
            {
                uriHelper.AttachJsRuntime(jsRuntime);
                uriHelper.InitializeState(
                    uriAbsolute,
                    baseUriAbsolute);

                navigationInterception.AttachJSRuntime(jsRuntime);
            }
            else
            {
                uriHelper.InitializeState(uriAbsolute, baseUriAbsolute);
            }

            var rendererRegistry = new RendererRegistry();
            var renderer = new RemoteRenderer(
                scope.ServiceProvider,
                _loggerFactory,
                rendererRegistry,
                jsRuntime,
                client,
                encoder,
                _loggerFactory.CreateLogger<RemoteRenderer>());

            var circuitHandlers = scope.ServiceProvider.GetServices<CircuitHandler>()
                .OrderBy(h => h.Order)
                .ToArray();

            var circuitHost = new CircuitHost(
                _circuitIdFactory.CreateCircuitId(),
                scope,
                client,
                rendererRegistry,
                renderer,
                components,
                jsRuntime,
                circuitHandlers,
                _loggerFactory.CreateLogger<CircuitHost>());
            Log.CreatedCircuit(_logger, circuitHost);

            // Initialize per - circuit data that services need
            (circuitHost.Services.GetRequiredService<ICircuitAccessor>() as DefaultCircuitAccessor).Circuit = circuitHost.Circuit;

            return circuitHost;
        }

        internal static IList<ComponentDescriptor> ResolveComponentMetadata(HttpContext httpContext, CircuitClientProxy client)
        {
            if (!client.Connected)
            {
                // This is the prerendering case. Descriptors will be registered by the prerenderer.
                return new List<ComponentDescriptor>();
            }
            else
            {
                var endpointFeature = httpContext.Features.Get<IEndpointFeature>();
                var endpoint = endpointFeature?.Endpoint;
                if (endpoint == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(ComponentHub)} doesn't have an associated endpoint. " +
                        "Use 'app.UseEndpoints(endpoints => endpoints.MapBlazorHub<App>(\"app\"))' to register your hub.");
                }

                var componentsMetadata = endpoint.Metadata.OfType<ComponentDescriptor>().ToList();

                return componentsMetadata;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, string, Exception> _createdConnectedCircuit =
                LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(1, "CreatedConnectedCircuit"), "Created circuit {CircuitId} for connection {ConnectionId}");

            private static readonly Action<ILogger, string, Exception> _createdDisconnectedCircuit =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, "CreatedDisconnectedCircuit"), "Created circuit {CircuitId} for disconnected client");

            internal static void CreatedCircuit(ILogger logger, CircuitHost circuitHost)
            {
                if (circuitHost.Client.Connected)
                {
                    _createdConnectedCircuit(logger, circuitHost.CircuitId, circuitHost.Client.ConnectionId, null);
                }
                else
                {
                    _createdDisconnectedCircuit(logger, circuitHost.CircuitId, null);
                }
            }
        }
    }
}
