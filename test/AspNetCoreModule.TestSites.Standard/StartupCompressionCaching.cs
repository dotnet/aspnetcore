// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;

namespace AspnetCoreModule.TestSites.Standard
{
    public class StartupCompressionCaching
    {
        public static bool CompressionMode = true;

        public void ConfigureServices(IServiceCollection services)
        {
            if (CompressionMode)
            {
                services.AddResponseCompression();
            }
            services.AddResponseCaching();
        } 

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            if (CompressionMode)
            {
                app.UseResponseCompression();
            }
            app.UseResponseCaching();
            app.UseDefaultFiles();
            app.UseStaticFiles(
                new StaticFileOptions()
                {
                    OnPrepareResponse = context =>
                    {
                        //
                        // FYI, below line can be simplified with 
                        //    context.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=10";
                        //
                        context.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                        {
                            Public = true,
                            MaxAge = TimeSpan.FromSeconds(10)
                        };
                        context.Context.Response.Headers.Append("MyCustomHeader", DateTime.Now.Second.ToString());
                        var accept = context.Context.Request.Headers[HeaderNames.AcceptEncoding];
                        if (!StringValues.IsNullOrEmpty(accept))
                        {
                            context.Context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);
                        }
                        context.Context.Response.ContentType = "text/plain";
                    }
                }
            ); 
        }
    }
}
