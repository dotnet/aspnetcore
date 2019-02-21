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
using Microsoft.AspNetCore.SignalR;
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
            IClientProxy client,
            string uriAbsolute,
            string baseUriAbsolute)
        {
            var components = ResolveComponentMetadata(httpContext, client);

            var scope = _scopeFactory.CreateScope();
            var encoder = scope.ServiceProvider.GetRequiredService<HtmlEncoder>();
            var jsRuntime = (RemoteJSRuntime)scope.ServiceProvider.GetRequiredService<IJSRuntime>();
            if (client != null)
            {
                jsRuntime.Initialize(client);
            }

            var uriHelper = (RemoteUriHelper)scope.ServiceProvider.GetRequiredService<IUriHelper>();
            if (client != null)
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
                circuitHandlers);

            // Initialize per - circuit data that services need
#pragma warning disable CS0618 // Type or member is obsolete
            (circuitHost.Services.GetRequiredService<IJSRuntimeAccessor>() as DefaultJSRuntimeAccessor).JSRuntime = jsRuntime;
#pragma warning restore CS0618 // Type or member is obsolete
            (circuitHost.Services.GetRequiredService<ICircuitAccessor>() as DefaultCircuitAccessor).Circuit = circuitHost.Circuit;

            return circuitHost;
        }

        private static IList<ComponentDescriptor> ResolveComponentMetadata(HttpContext httpContext, IClientProxy client)
        {
            if (client != null)
            {
                var endpointFeature = httpContext.Features.Get<IEndpointFeature>();
                var endpoint = endpointFeature?.Endpoint;
                if (endpoint == null)
                {
                    throw new InvalidOperationException("CompnentHub doesn't have an associated endpoint.");
                }

                var componentsMetadata = endpoint.Metadata.OfType<ComponentDescriptor>().ToList();
                if (componentsMetadata.Count == 0)
                {
                    throw new InvalidOperationException("No component was added to the component hub.");
                }
                else
                {
                    return componentsMetadata;
                }
            }
            else
            {
                // This is the prerrendering case.
                return Array.Empty<ComponentDescriptor>();
            }
        }
    }
}
