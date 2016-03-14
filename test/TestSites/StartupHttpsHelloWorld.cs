// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TestSites
{
    public class StartupHttpsHelloWorld
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.Run(ctx =>
            {
                if (ctx.Request.Path.Equals(new PathString("/checkclientcert")))
                {
                    return ctx.Response.WriteAsync("Scheme:" + ctx.Request.Scheme + "; Forwarded:" + ctx.Request.Headers["x-forwarded-proto"]
                        + "; has cert? " + (ctx.Connection.ClientCertificate != null));
                }
                return ctx.Response.WriteAsync("Scheme:" + ctx.Request.Scheme + "; Forwarded:" + ctx.Request.Headers["x-forwarded-proto"]);
            });
        }
    }
}