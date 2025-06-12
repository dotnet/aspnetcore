// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Hosting;
using TlsFeaturesObserve.HttpSys;

HttpSysConfigurator.ConfigureCacheTlsClientHello();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseHttpSys(options =>
{
    options.UrlPrefixes.Add("https://*:6000");
    options.Authentication.Schemes = AuthenticationSchemes.None;
    options.Authentication.AllowAnonymous = true;
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var connectionFeature = context.Features.GetRequiredFeature<IHttpConnectionFeature>();
    var httpSysPropFeature = context.Features.GetRequiredFeature<IHttpSysRequestPropertyFeature>();

    // first time invocation to find out required size
    var success = httpSysPropFeature.TryGetTlsClientHello(Array.Empty<byte>(), out var bytesReturned);
    Debug.Assert(!success);
    Debug.Assert(bytesReturned > 0);

    // rent with enough memory span and invoke
    var bytes = ArrayPool<byte>.Shared.Rent(bytesReturned);
    success = httpSysPropFeature.TryGetTlsClientHello(bytes, out _);
    Debug.Assert(success);

    await context.Response.WriteAsync($"[Response] connectionId={connectionFeature.ConnectionId}; tlsClientHello.length={bytesReturned}; tlsclienthello start={string.Join(' ', bytes.AsSpan(0, 30).ToArray())}");
    await next(context);
});

app.Run();
