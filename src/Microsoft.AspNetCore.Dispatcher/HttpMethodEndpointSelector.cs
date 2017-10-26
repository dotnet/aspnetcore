// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class HttpMethodEndpointSelector : EndpointSelector
    {
        private object _lock;
        private bool _servicesInitialized;

        public HttpMethodEndpointSelector()
        {
            _lock = new object();
        }

        protected ILogger Logger { get; private set; }

        public override async Task SelectAsync(EndpointSelectorContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureServicesInitialized(context);

            var snapshot = context.CreateSnapshot();

            var fallbackEndpoints = new List<Endpoint>();
            for (var i = context.Endpoints.Count - 1; i >= 0; i--)
            {
                var endpoint = context.Endpoints[i] as IRoutePatternEndpoint;
                if (endpoint == null || endpoint.HttpMethod == null)
                {
                    // No metadata.
                    Logger.NoHttpMethodFound(context.Endpoints[i]);

                    fallbackEndpoints.Add(context.Endpoints[i]);
                    context.Endpoints.RemoveAt(i);
                }
                else if (string.Equals(endpoint.HttpMethod, context.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    // The request method matches the endpoint's HTTP method.
                    Logger.RequestMethodMatchedEndpointMethod(endpoint.HttpMethod, context.Endpoints[i]);
                }
                else
                {
                    // Not a match.
                    Logger.RequestMethodDidNotMatchEndpointMethod(context.HttpContext.Request.Method, endpoint.HttpMethod, context.Endpoints[i]);
                    context.Endpoints.RemoveAt(i);
                }
            }

            // Now the list of endpoints only contains those that have an HTTP method preference AND match the current
            // request.
            await context.InvokeNextAsync();

            if (context.Endpoints.Count == 0)
            {
                Logger.NoEndpointMatchedRequestMethod(context.HttpContext.Request.Method);

                // Nothing matched, do the fallback.
                context.RestoreSnapshot(snapshot);

                context.Endpoints.Clear();

                for (var i = 0; i < fallbackEndpoints.Count; i++)
                {
                    context.Endpoints.Add(fallbackEndpoints[i]);
                }

                await context.InvokeNextAsync();
            }
        }

        protected void EnsureServicesInitialized(EndpointSelectorContext context)
        {
            if (Volatile.Read(ref _servicesInitialized))
            {
                return;
            }

            EnsureServicesInitializedSlow(context);
        }

        private void EnsureServicesInitializedSlow(EndpointSelectorContext context)
        {
            lock (_lock)
            {
                if (!Volatile.Read(ref _servicesInitialized))
                {
                    var services = context.HttpContext.RequestServices;
                    Logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
                }
            }
        }
    }
}
