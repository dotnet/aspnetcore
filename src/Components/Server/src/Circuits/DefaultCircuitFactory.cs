// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Browser;
using Microsoft.AspNetCore.Components.Browser.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Services;
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

        public DefaultCircuitFactory(
            IServiceScopeFactory scopeFactory,
            ILoggerFactory loggerFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _loggerFactory = loggerFactory;
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

            var uriHelper = (RemoteUriHelper)scope.ServiceProvider.GetRequiredService<IUriHelper>();
            if (client != CircuitClientProxy.OfflineClient)
            {
                uriHelper.Initialize(uriAbsolute, baseUriAbsolute, jsRuntime);
            }
            else
            {
                uriHelper.Initialize(uriAbsolute, baseUriAbsolute);
            }

            var rendererRegistry = new RendererRegistry();
            var dispatcher = Renderer.CreateDefaultDispatcher();
            var renderer = new RemoteRenderer(
                scope.ServiceProvider,
                rendererRegistry,
                jsRuntime,
                client,
                dispatcher,
                encoder,
                _loggerFactory.CreateLogger<RemoteRenderer>());

            var circuitHandlers = scope.ServiceProvider.GetServices<CircuitHandler>()
                .OrderBy(h => h.Order)
                .ToArray();

            var circuitHost = new CircuitHost(
                scope,
                client,
                rendererRegistry,
                renderer,
                components,
                dispatcher,
                jsRuntime,
                circuitHandlers,
                _loggerFactory.CreateLogger<CircuitHost>());

            // Initialize per - circuit data that services need
            (circuitHost.Services.GetRequiredService<ICircuitAccessor>() as DefaultCircuitAccessor).Circuit = circuitHost.Circuit;

            return circuitHost;
        }

        private static IList<ComponentDescriptor> ResolveComponentMetadata(HttpContext httpContext, CircuitClientProxy client)
        {
            if (client == CircuitClientProxy.OfflineClient)
            {
                // This is the prerendering case.
                return Array.Empty<ComponentDescriptor>();
            }
            else
            {
                var endpointFeature = httpContext.Features.Get<IEndpointFeature>();
                var endpoint = endpointFeature?.Endpoint;
                if (endpoint == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(ComponentHub)} doesn't have an associated endpoint. " +
                        "Use 'app.UseEndpoints(endpoints => endpoints.MapComponentHub<App>(\"app\"))' to register your hub.");
                }

                var componentsMetadata = endpoint.Metadata.OfType<ComponentDescriptor>().ToList();
                if (componentsMetadata.Count == 0)
                {
                    throw new InvalidOperationException("No component was registered with the component hub.");
                }

                return componentsMetadata;
            }
        }
    }
}
