// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class CircuitFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CircuitIdFactory _circuitIdFactory;
        private readonly CircuitOptions _options;
        private readonly ILogger _logger;

        public CircuitFactory(
            IServiceScopeFactory scopeFactory,
            ILoggerFactory loggerFactory,
            CircuitIdFactory circuitIdFactory,
            IOptions<CircuitOptions> options)
        {
            _scopeFactory = scopeFactory;
            _loggerFactory = loggerFactory;
            _circuitIdFactory = circuitIdFactory;
            _options = options.Value;
            _logger = _loggerFactory.CreateLogger<CircuitFactory>();
        }

        public CircuitHost CreateCircuitHost(
            IReadOnlyList<ComponentDescriptor> components,
            CircuitClientProxy client,
            string baseUri,
            string uri,
            ClaimsPrincipal user)
        {
            var scope = _scopeFactory.CreateScope();
            var jsRuntime = (RemoteJSRuntime)scope.ServiceProvider.GetRequiredService<IJSRuntime>();
            jsRuntime.Initialize(client);

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
                client,
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

        private static class Log
        {
            private static readonly Action<ILogger, string, string, Exception> _createdCircuit =
                LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(1, "CreatedCircuit"), "Created circuit {CircuitId} for connection {ConnectionId}");

            internal static void CreatedCircuit(ILogger logger, CircuitHost circuitHost) =>
                _createdCircuit(logger, circuitHost.CircuitId.Id, circuitHost.Client.ConnectionId, null);
        }
    }
}
