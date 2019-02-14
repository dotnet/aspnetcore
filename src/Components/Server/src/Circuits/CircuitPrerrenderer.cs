// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class CircuitPrerrenderer : IComponentPrerrenderer
    {
        private readonly CircuitFactory _circuitFactory;

        public CircuitPrerrenderer(CircuitFactory circuitFactory)
        {
            _circuitFactory = circuitFactory;
        }

        public async Task<IEnumerable<string>> PrerrenderComponentAsync(ComponentPrerrenderingContext prerrenderingContext)
        {
            var context = prerrenderingContext.Context;
            var circuitHost = _circuitFactory.CreateCircuitHost(
                context,
                client: null,
                GetFullUri(context.Request),
                GetFullBaseUri(context.Request));

            // For right now we just do prerrendering and dispose the circuit. In the future we will keep the circuit around and
            // reconnect to it from the ComponentsHub.
            try
            {
                var result = await circuitHost.PrerrenderComponentAsync(
                prerrenderingContext.ComponentType,
                prerrenderingContext.Parameters);
                return result;
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
