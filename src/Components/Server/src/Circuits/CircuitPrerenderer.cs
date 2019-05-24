// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class CircuitPrerenderer : IComponentPrerenderer
    {
        private static object CircuitHostKey = new object();

        private readonly CircuitFactory _circuitFactory;
        private readonly CircuitRegistry _registry;

        public CircuitPrerenderer(CircuitFactory circuitFactory, CircuitRegistry registry)
        {
            _circuitFactory = circuitFactory;
            _registry = registry;
        }

        public async Task<ComponentPrerenderResult> PrerenderComponentAsync(ComponentPrerenderingContext prerenderingContext)
        {
            var context = prerenderingContext.Context;
            var circuitHost = GetOrCreateCircuitHost(context);

            var renderResult = await circuitHost.PrerenderComponentAsync(
                prerenderingContext.ComponentType,
                prerenderingContext.Parameters);

            circuitHost.Descriptors.Add(new ComponentDescriptor
            {
                ComponentType = prerenderingContext.ComponentType,
                Prerendered = true
            });

            var result = new[] {
                $"<!-- M.A.C.Component:{{\"circuitId\":\"{circuitHost.CircuitId}\",\"rendererId\":\"{circuitHost.Renderer.Id}\",\"componentId\":\"{renderResult.ComponentId}\"}} -->",
            }.Concat(renderResult.Tokens).Concat(
                new[] {
                    $"<!-- M.A.C.Component: {renderResult.ComponentId} -->"
                });

            return new ComponentPrerenderResult(result);
        }

        private CircuitHost GetOrCreateCircuitHost(HttpContext context)
        {
            if (context.Items.TryGetValue(CircuitHostKey, out var existingHost))
            {
                return (CircuitHost)existingHost;
            }
            else
            {
                var result = _circuitFactory.CreateCircuitHost(
                    context,
                    client: new CircuitClientProxy(), // This creates an "offline" client.
                    GetFullUri(context.Request),
                    GetFullBaseUri(context.Request));

                result.UnhandledException += CircuitHost_UnhandledException;
                context.Response.OnCompleted(() =>
                {
                    result.UnhandledException -= CircuitHost_UnhandledException;
                    _registry.RegisterDisconnectedCircuit(result);
                    return Task.CompletedTask;
                });
                context.Items.Add(CircuitHostKey, result);

                return result;
            }
        }

        private void CircuitHost_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Throw all exceptions encountered during pre-rendering so the default developer
            // error page can respond.
            ExceptionDispatchInfo.Capture((Exception)e.ExceptionObject).Throw();
        }

        private string GetFullUri(HttpRequest request)
        {
            return UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase,
                request.Path,
                request.QueryString);
        }

        private string GetFullBaseUri(HttpRequest request)
        {
            var result = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);

            // PathBase may be "/" or "/some/thing", but to be a well-formed base URI
            // it has to end with a trailing slash
            if (!result.EndsWith('/'))
            {
                result += '/';
            }

            return result;
        }
    }
}
