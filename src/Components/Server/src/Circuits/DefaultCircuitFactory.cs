// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class DefaultCircuitFactory : CircuitFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CircuitIdFactory _circuitIdFactory;
        private readonly CircuitOptions _options;
        private readonly ILogger _logger;

        public DefaultCircuitFactory(
            IServiceScopeFactory scopeFactory,
            ILoggerFactory loggerFactory,
            CircuitIdFactory circuitIdFactory,
            IOptions<CircuitOptions> options)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _circuitIdFactory = circuitIdFactory ?? throw new ArgumentNullException(nameof(circuitIdFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _logger = _loggerFactory.CreateLogger<DefaultCircuitFactory>();
        }

        public override CircuitHost CreateCircuitHost(
            HttpContext httpContext,
            CircuitClientProxy client,
            string baseUri,
            string uri,
            ClaimsPrincipal user)
        {
            // We do as much intialization as possible eagerly in this method, which makes the error handling
            // story much simpler. If we throw from here, it's handled inside the initial hub method.
            var components = ResolveComponentMetadata(httpContext);

            var scope = _scopeFactory.CreateScope();
            var encoder = scope.ServiceProvider.GetRequiredService<HtmlEncoder>();
            var jsRuntime = (RemoteJSRuntime)scope.ServiceProvider.GetRequiredService<IJSRuntime>();
            var componentContext = (RemoteComponentContext)scope.ServiceProvider.GetRequiredService<IComponentContext>();
            jsRuntime.Initialize(client);
            componentContext.Initialize(client);

            var navigationManager = (RemoteNavigationManager)scope.ServiceProvider.GetRequiredService<NavigationManager>();
            var navigationInterception = (RemoteNavigationInterception)scope.ServiceProvider.GetRequiredService<INavigationInterception>();
            if (client.Connected)
            {
                navigationManager.AttachJsRuntime(jsRuntime);
                navigationManager.Initialize(baseUri, uri);

                navigationInterception.AttachJSRuntime(jsRuntime);
            }
            else
            {
                navigationManager.Initialize(baseUri, uri);
            }

            var renderer = new RemoteRenderer(
                scope.ServiceProvider,
                _loggerFactory,
                _options,
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
                _options,
                client,
                renderer,
                components,
                jsRuntime,
                circuitHandlers,
                _loggerFactory.CreateLogger<CircuitHost>());
            Log.CreatedCircuit(_logger, circuitHost);

            // Initialize per - circuit data that services need
            (circuitHost.Services.GetRequiredService<ICircuitAccessor>() as DefaultCircuitAccessor).Circuit = circuitHost.Circuit;
            circuitHost.SetCircuitUser(user);

            return circuitHost;
        }

        public static IReadOnlyList<ComponentDescriptor> ResolveComponentMetadata(HttpContext httpContext)
        {
            var endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ComponentHub)} doesn't have an associated endpoint. " +
                    "Use 'app.UseEndpoints(endpoints => endpoints.MapBlazorHub<App>(\"app\"))' to register your hub.");
            }

            return endpoint.Metadata.GetOrderedMetadata<ComponentDescriptor>();
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
