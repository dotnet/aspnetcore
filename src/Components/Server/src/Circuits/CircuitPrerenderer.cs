// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class CircuitPrerenderer : IComponentPrerenderer
    {
        private readonly CircuitFactory _circuitFactory;

        public CircuitPrerenderer(CircuitFactory circuitFactory)
        {
            _circuitFactory = circuitFactory;
        }

        public async Task<IEnumerable<string>> PrerenderComponentAsync(ComponentPrerenderingContext prerenderingContext)
        {
            var context = prerenderingContext.Context;
            var circuitHost = _circuitFactory.CreateCircuitHost(
                context,
                client: CircuitClientProxy.OfflineClient,
                GetFullUri(context.Request),
                GetFullBaseUri(context.Request));

            // For right now we just do prerendering and dispose the circuit. In the future we will keep the circuit around and
            // reconnect to it from the ComponentsHub.
            try
            {
                return await circuitHost.PrerenderComponentAsync(
                    prerenderingContext.ComponentType,
                    prerenderingContext.Parameters);
            }
            finally
            {
                await circuitHost.DisposeAsync();
            }
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
            return UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);
        }
    }
}
