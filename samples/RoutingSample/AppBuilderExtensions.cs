﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Owin;
using Owin;

namespace RoutingSample
{
    public static class AppBuilderExtensions
    {
        public static IRouteCollection UseRouter(this IAppBuilder app)
        {
            IRouteCollection routes = null;
            app.UseBuilder((b) => routes = b.UseRouter());
            return routes;
        }
    }
}
