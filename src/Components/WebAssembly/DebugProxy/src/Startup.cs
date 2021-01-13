// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAssembly.Net.Debugging;

namespace Microsoft.AspNetCore.Components.WebAssembly.DebugProxy
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, DebugProxyOptions debugProxyOptions)
        {
            app.UseDeveloperExceptionPage();
            app.UseWebSockets();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // At the homepage, we check whether we can uniquely identify the target tab
                //  - If yes, we redirect directly to the debug tools, proxying to that tab
                //  - If no, we present a list of available tabs for the user to pick from
                endpoints.MapGet("/", new TargetPickerUi(debugProxyOptions).Display);

                // At this URL, we wire up the actual WebAssembly proxy
                endpoints.MapGet("/ws-proxy", async (context) =>
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                    var browserUri = new Uri(context.Request.Query["browser"]);
                    var ideSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await new MonoProxy(loggerFactory).Run(browserUri, ideSocket);
                });
            });
        }
    }
}
