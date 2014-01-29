﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing.Owin;
using Owin;

namespace RoutingSample
{
    internal class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var routes = appBuilder.UseRouter();

            OwinRouteEndpoint endpoint1 = new OwinRouteEndpoint(async (context) => await WriteToBodyAsync(context, "match1"));
            OwinRouteEndpoint endpoint2 = new OwinRouteEndpoint(async (context) => await WriteToBodyAsync(context, "Hello, World!"));

            routes.Add(new PrefixRoute(endpoint1, "api/store"));
            routes.Add(new PrefixRoute(endpoint1, "api/checkout"));
            routes.Add(new PrefixRoute(endpoint2, "hello/world"));
            routes.Add(new PrefixRoute(endpoint1, ""));
        }

        private static async Task WriteToBodyAsync(IDictionary<string, object> context, string text)
        {
            var stream = (Stream)context["owin.ResponseBody"];

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
