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

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseHttpSys(options =>
{
    options.UrlPrefixes.Add("http://*:42000");

    options.Authentication.Schemes = AuthenticationSchemes.None;
    options.Authentication.AllowAnonymous = true;
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var connectionFeature = context.Features.GetRequiredFeature<IHttpConnectionFeature>();

    var response = $"""
        ConnectionInfo:
        - HttpContext.Connection.LocalPort  : {context.Connection.LocalPort}
        - HttpContext.Connection.RemotePort : {context.Connection.RemotePort}
        - IHttpConnectionFeature.LocalPort  : {connectionFeature.LocalPort}
        - IHttpConnectionFeature.RemotePort : {connectionFeature.RemotePort}
    """;

    await context.Response.WriteAsync(response);
    await next(context);
});

app.Run();
