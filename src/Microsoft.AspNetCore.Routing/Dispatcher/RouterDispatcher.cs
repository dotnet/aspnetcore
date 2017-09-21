// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    /// <summary>
    /// An adapter to plug an <see cref="IRouter"/> into a dispatcher.
    /// </summary>
    public class RouterDispatcher : DispatcherBase
    {
        private readonly Endpoint _fallbackEndpoint;
        private readonly IRouter _router;

        public RouterDispatcher(IRouter router)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            _router = router;
            _fallbackEndpoint = new UnknownEndpoint(_router);
        }

        protected override async Task<bool> TryMatchAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }
            
            var routeContext = new RouteContext(httpContext);
            await _router.RouteAsync(routeContext);

            var feature = httpContext.Features.Get<IDispatcherFeature>();
            if (routeContext.Handler == null)
            {
                // The route did not match, clear everything as it may have been set by the route.
                feature.Endpoint = null;
                feature.RequestDelegate = null;
                feature.Values = null;
                return false;
            }
            else
            {
                feature.Endpoint = feature.Endpoint ?? _fallbackEndpoint;
                feature.RequestDelegate = routeContext.Handler;
                feature.Values = routeContext.RouteData.Values;
                return true;
            }
        }

        private class UnknownEndpoint : Endpoint
        {
            public UnknownEndpoint(IRouter router)
            {
                DisplayName = $"Endpoint for '{router}";
            }

            public override string DisplayName { get; }

            public override IReadOnlyList<object> Metadata => Array.Empty<object>();
        }
    }
}

