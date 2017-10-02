// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IISTestSite
{
    public class StartupResponseInvalidOrdering
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/SetStatusCodeAfterWrite"))
                {
                    await context.Response.WriteAsync("Started_");
                    try
                    {
                        context.Response.StatusCode = 200;
                    }
                    catch (InvalidOperationException)
                    {
                        await context.Response.WriteAsync("SetStatusCodeAfterWriteThrew_");
                    }
                    await context.Response.WriteAsync("Finished");
                    return;
                }
                else if (context.Request.Path.Equals("/SetHeaderAfterWrite"))
                {
                    await context.Response.WriteAsync("Started_");
                    try
                    {
                        context.Response.Headers["This will fail"] = "some value";
                    }
                    catch (InvalidOperationException)
                    {
                        await context.Response.WriteAsync("SetHeaderAfterWriteThrew_");
                    }
                    await context.Response.WriteAsync("Finished");
                    return;
                }
            });
        }
    }
}
