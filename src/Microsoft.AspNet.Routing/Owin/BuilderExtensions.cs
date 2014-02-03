﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing.Owin
{
    public static class BuilderExtensions
    {
        public static IRouteCollection UseRouter(this IBuilder builder)
        {
            var routes = new DefaultRouteCollection();
            var engine = new DefaultRouteEngine(routes);

            builder.Use((next) => new RouterMiddleware(next, engine).Invoke);

            return routes;
        }
    }
}

