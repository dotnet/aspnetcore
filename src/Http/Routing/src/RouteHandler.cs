// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Supports implementing a handler that executes for a given route.
    /// </summary>
    public class RouteHandler : IRouteHandler, IRouter
    {
        private readonly RequestDelegate _requestDelegate;

        /// <summary>
        /// Constructs a new <see cref="RouteHandler"/> instance.
        /// </summary>
        /// <param name="requestDelegate">The delegate used to process requests.</param>
        public RouteHandler(RequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        /// <inheritdoc />
        public RequestDelegate GetRequestHandler(HttpContext httpContext, RouteData routeData)
        {
            return _requestDelegate;
        }

        /// <inheritdoc />
        public VirtualPathData? GetVirtualPath(VirtualPathContext context)
        {
            // Nothing to do.
            return null;
        }

        /// <inheritdoc />
        public Task RouteAsync(RouteContext context)
        {
            context.Handler = _requestDelegate;
            return Task.CompletedTask;
        }
    }
}
