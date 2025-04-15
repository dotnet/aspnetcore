// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TlsFeatureObserve;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.Run(async (HttpContext context) =>
        {
            context.Response.ContentType = "text/plain";

            var tlsFeature = context.Features.Get<IMyTlsFeature>();
            await context.Response.WriteAsync("TlsClientHello data: " + $"connectionId={tlsFeature?.ConnectionId}; length={tlsFeature?.TlsClientHelloLength}");
        });
    }
}
