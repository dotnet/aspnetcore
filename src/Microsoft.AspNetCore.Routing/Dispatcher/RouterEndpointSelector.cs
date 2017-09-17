// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    public class RouterEndpointSelector : IRouter, IRouteHandler
    {
        private readonly RouteValuesEndpoint[] _endpoints;

        public RouterEndpointSelector(IEnumerable<RouteValuesEndpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            _endpoints = endpoints.ToArray();
        }

        public RequestDelegate GetRequestHandler(HttpContext httpContext, RouteData routeData)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (routeData == null)
            {
                throw new ArgumentNullException(nameof(routeData));
            }

            var dispatcherFeature = httpContext.Features.Get<IDispatcherFeature>();
            if (dispatcherFeature == null)
            {
                throw new InvalidOperationException(Resources.FormatDispatcherFeatureIsRequired(
                    nameof(HttpContext),
                    nameof(IDispatcherFeature),
                    nameof(RouterEndpointSelector)));
            }

            for (var i = 0; i < _endpoints.Length; i++)
            {
                var endpoint = _endpoints[i];
                if (CompareRouteValues(routeData.Values, endpoint.RequiredValues))
                {
                    dispatcherFeature.Endpoint = endpoint;
                    return null;
                }
            }

            return null;
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var handler = GetRequestHandler(context.HttpContext, context.RouteData);
            if (handler != null)
            {
                context.Handler = handler;
            }

            return Task.CompletedTask;
        }

        private bool CompareRouteValues(RouteValueDictionary values, RouteValueDictionary requiredValues)
        {
            foreach (var kvp in requiredValues)
            {
                if (string.IsNullOrEmpty(kvp.Value.ToString()))
                {
                    if (values.TryGetValue(kvp.Key, out var routeValue) && !string.IsNullOrEmpty(routeValue.ToString()))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!values.TryGetValue(kvp.Key, out var routeValue) || !string.Equals(kvp.Value.ToString(), routeValue.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
