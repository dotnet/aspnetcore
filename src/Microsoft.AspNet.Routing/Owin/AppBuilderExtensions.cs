﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NET45

using Owin;

namespace Microsoft.AspNet.Routing.Owin
{
    public static class AppBuilderExtensions
    {
        public static IRouteCollection UseRouter(this IAppBuilder builder)
        {
            var routes = new DefaultRouteCollection();
            var engine = new DefaultRouteEngine(routes);

            builder.Use(typeof(RouterMiddleware), engine);

            return routes;
        }
    }
}

#endif
