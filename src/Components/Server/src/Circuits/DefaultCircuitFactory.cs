// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Browser;
using Microsoft.AspNetCore.Components.Browser.Rendering;
using Microsoft.AspNetCore.Components.Environment;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class DefaultCircuitFactory : CircuitFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DefaultCircuitFactoryOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        public DefaultCircuitFactory(
            IServiceScopeFactory scopeFactory,
            IOptions<DefaultCircuitFactoryOptions> options,
            ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _options = options.Value;
            _loggerFactory = loggerFactory;
        }

        public override CircuitHost CreateCircuitHost(HttpContext httpContext, IClientProxy client)
        {
            if (!_options.StartupActions.TryGetValue(httpContext.Request.Path, out var config))
            {
                var message = $"Could not find an ASP.NET Core Components startup action for request path '{httpContext.Request.Path}'.";
                throw new InvalidOperationException(message);
            }

            var scope = _scopeFactory.CreateScope();
            var environment = scope.ServiceProvider.GetRequiredService<ComponentEnvironment>();
            InitializeEnvironment(environment, client);

            var rendererRegistry = new RendererRegistry();
            var dispatcher = Renderer.CreateDefaultDispatcher();
            var renderer = new RemoteRenderer(
                scope.ServiceProvider,
                rendererRegistry,
                environment.JSRuntime,
                client,
                dispatcher,
                _loggerFactory.CreateLogger<RemoteRenderer>());

            var circuitHandlers = scope.ServiceProvider.GetServices<CircuitHandler>()
                .OrderBy(h => h.Order)
                .ToArray();

            var circuitHost = new CircuitHost(
                scope,
                client,
                rendererRegistry,
                renderer,
                config,
                environment.JSRuntime,
                circuitHandlers);

            // Initialize per-circuit data that services need
#pragma warning disable CS0618 // Type or member is obsolete
            (circuitHost.Services.GetRequiredService<IJSRuntimeAccessor>() as DefaultJSRuntimeAccessor).JSRuntime = environment.JSRuntime;
#pragma warning restore CS0618 // Type or member is obsolete
            (circuitHost.Services.GetRequiredService<ICircuitAccessor>() as DefaultCircuitAccessor).Circuit = circuitHost.Circuit;

            return circuitHost;
        }

        private static void InitializeEnvironment(ComponentEnvironment environment, IClientProxy client)
        {
            environment.Name = ComponentEnvironment.Remote;
            environment.JSRuntime = new RemoteJSRuntime(client);
            environment.UriHelper = new RemoteUriHelper(environment.JSRuntime);
        }
    }
}
