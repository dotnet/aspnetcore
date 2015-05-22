// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace ResponseCachingSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCaching();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseResponseCaching();
            app.Run(async (context) =>
            {
                context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)                    
                };
                await context.Response.WriteAsync("Hello World! " + DateTime.UtcNow);
            });
        }
    }
}
