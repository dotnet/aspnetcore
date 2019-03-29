// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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

            // We don't need to unsubscribe because the circuit host object is scoped to this call.
            circuitHost.UnhandledException += CircuitHost_UnhandledException;

            // For right now we just do prerendering and dispose the circuit. In the future we will keep the circuit around and
            // reconnect to it from the ComponentsHub. If we keep the circuit/renderer we also need to unsubscribe this error
            // handler.
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
