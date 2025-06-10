// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Reflection;
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
        // recommended approach to fetch TLS client hello bytes
        // is via on-demand API per request or by building own connection-lifecycle manager
        app.Run(async (HttpContext context) =>
        {
            context.Response.ContentType = "text/plain";

            var httpSysAssembly = typeof(Microsoft.AspNetCore.Server.HttpSys.HttpSysOptions).Assembly;
            var httpSysPropertyFeatureType = httpSysAssembly.GetType("Microsoft.AspNetCore.Server.HttpSys.IHttpSysRequestPropertyFeature");
            var httpSysPropertyFeature = context.Features[httpSysPropertyFeatureType]!;

            var method = httpSysPropertyFeature.GetType().GetMethod(
                "TryGetTlsClientHello",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            // invoke first time to get required size
            byte[] bytes = Array.Empty<byte>();
            var parameters = new object[] { bytes, 0 };
            var res = (bool)method.Invoke(httpSysPropertyFeature, parameters);

            // fetching out parameter only works by looking into parameters array of objects
            var bytesReturned = (int)parameters[1];
            bytes = ArrayPool<byte>.Shared.Rent(bytesReturned);
            parameters = [bytes, 0]; // correct input now
            res = (bool)method.Invoke(httpSysPropertyFeature, parameters);

            // this is the span representing the TLS Client Hello bytes only
            var tlsClientHelloBytes = ((byte[])parameters[0]).AsSpan(0, bytesReturned);
            await context.Response.WriteAsync($"TlsBytes: {string.Join(" ", tlsClientHelloBytes.Slice(0, 10).ToArray())}; full length = {bytesReturned}");

            ArrayPool<byte>.Shared.Return(bytes);
        });

        // middleware compatible with callback API
        //app.Run(async (HttpContext context) =>
        //{
        //    context.Response.ContentType = "text/plain";

        //    var tlsFeature = context.Features.Get<IMyTlsFeature>();
        //    await context.Response.WriteAsync("TlsClientHello` data: " + $"connectionId={tlsFeature?.ConnectionId}; length={tlsFeature?.TlsClientHelloLength}");
        //});
    }
}
