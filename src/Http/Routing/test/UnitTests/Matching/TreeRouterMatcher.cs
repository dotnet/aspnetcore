// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Tree;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // This is an adapter to use TreeRouter in the conformance tests
    internal class TreeRouterMatcher : Matcher
    {
        private readonly TreeRouter _inner;

        internal TreeRouterMatcher(TreeRouter inner)
        {
            _inner = inner;
        }

        public async override Task MatchAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var routeContext = new RouteContext(httpContext);
            await _inner.RouteAsync(routeContext);

            if (routeContext.Handler != null)
            {
                httpContext.Request.RouteValues = routeContext.RouteData.Values;
                await routeContext.Handler(httpContext);
            }
        }
    }
}

