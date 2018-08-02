// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // This is an adapter to use Route in the conformance tests
    internal class RouteMatcher : Matcher
    {
        private readonly RouteCollection _inner;

        internal RouteMatcher(RouteCollection inner)
        {
            _inner = inner;
        }

        public async override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var context = new RouteContext(httpContext);
            await _inner.RouteAsync(context);

            if (context.Handler != null)
            {
                feature.Values = context.RouteData.Values;
                await context.Handler(httpContext);
            }
        }
    }
}

