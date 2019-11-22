// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServerComparison.TestSites
{
    public class StartupResponseCompression
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            // NGinx's default min size is 20 bytes
            var helloWorldBody = "Hello World;" + new string('a', 20);

            app.Map("/NoAppCompression", subApp =>
            {
                subApp.Run(context =>
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.ContentLength = helloWorldBody.Length;
                    return context.Response.WriteAsync(helloWorldBody);
                });
            });

            app.Map("/AppCompression", subApp =>
            {
                subApp.UseResponseCompression();
                subApp.Run(context =>
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.ContentLength = helloWorldBody.Length;
                    return context.Response.WriteAsync(helloWorldBody);
                });
            });
            /* If we implement DisableResponseBuffering on IISMiddleware
            app.Map("/NoBuffer", subApp =>
            {
                subApp.UseResponseCompression();
                subApp.Run(context =>
                {
                    context.Features.Get<IHttpBufferingFeature>().DisableResponseBuffering();
                    context.Response.ContentType = "text/plain";
                    context.Response.ContentLength = helloWorldBody.Length;
                    return context.Response.WriteAsync(helloWorldBody);
                });
            });
            */
            app.Run(context =>
            {
                context.Response.ContentType = "text/plain";
                string body;
                if (context.Request.Path.Value == "/")
                {
                    body = "Running";
                }
                else
                {
                    body = "Not Implemented: " + context.Request.Path;
                }

                context.Response.ContentLength = body.Length;
                return context.Response.WriteAsync(body);
            });
        }
    }
}