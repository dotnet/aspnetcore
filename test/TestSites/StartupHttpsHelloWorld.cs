// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;

namespace TestSites
{
    public class StartupHttpsHelloWorld
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseIISPlatformHandler();
            app.Run(ctx =>
            {
                if (ctx.Request.Path.Equals(new PathString("/checkclientcert")))
                {
                    return ctx.Response.WriteAsync(ctx.Request.Scheme + " Hello World, has cert? " + (ctx.Connection.ClientCertificate != null));
                }
                return ctx.Response.WriteAsync(ctx.Request.Scheme + " Hello World");
            });
        }
    }
}