﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NET45

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing.Owin;
using Owin;
using Microsoft.AspNet.PipelineCore.Owin;
using Microsoft.AspNet.Routing;
using System;
using Microsoft.AspNet.Abstractions;

namespace RoutingSample
{
    internal class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder.UseErrorPage();

            builder.UseBuilder(ConfigureRoutes);
        }

        private void ConfigureRoutes(IBuilder builder)
        {            
            var routes = builder.UseRouter();

            var endpoint1 = new OwinRouteEndpoint(async (context) => await WriteToBodyAsync(context, "match1"));
            var endpoint2 = new HttpContextRouteEndpoint(async (context) => await context.Response.WriteAsync("Hello, World!"));

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

#endif
