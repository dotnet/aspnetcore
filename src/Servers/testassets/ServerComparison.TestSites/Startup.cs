// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ServerComparison.TestSites;

public class Startup
{
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.Map("/throwexception", subApp =>
        {
            subApp.Run(context =>
            {
                throw new ApplicationException("Application exception");
            });
        });

        app.Run(ctx =>
        {
            return ctx.Response.WriteAsync("Hello World " + RuntimeInformation.ProcessArchitecture);
        });
    }
}
