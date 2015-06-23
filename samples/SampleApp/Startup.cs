// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace SampleApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                Console.WriteLine("{0} {1}{2}{3}",
                    context.Request.Method,
                    context.Request.PathBase,
                    context.Request.Path,
                    context.Request.QueryString);

                context.Response.ContentLength = 11;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello world");
            });
        }
    }
}