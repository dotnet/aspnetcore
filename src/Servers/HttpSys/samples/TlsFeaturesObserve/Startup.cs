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

            var tlsFingerprintingFeature = context.Features.Get<ITlsFingerprintingFeature>();
            if (tlsFingerprintingFeature is null)
            {
                await context.Response.WriteAsync(nameof(ITlsFingerprintingFeature) + " is not resolved from " + nameof(context));
                return;
            }

            var tlsClientHello = tlsFingerprintingFeature.GetTlsClientHello();
            await context.Response.WriteAsync("TLS CLIENT HELLO: " + tlsClientHello);
        });
    }
}
