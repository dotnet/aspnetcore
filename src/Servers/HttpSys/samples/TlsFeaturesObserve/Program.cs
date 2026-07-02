// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
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

    // enable ITlsChannelBindingFeature
    options.EnableTlsChannelBinding = true;
});

var app = builder.Build();

// Example middleware using ITlsChannelBindingFeature to load the channel binding token bytes and parse them.
app.Use(async (context, next) =>
{
    var cbtFeature = context.Features.Get<ITlsChannelBindingFeature>();
    if (cbtFeature is not null)
    {
        var token = cbtFeature.GetChannelBindingBytes(ChannelBindingKind.Endpoint);
        if (token is { } bytes)
        {
            // Parse the SEC_CHANNEL_BINDINGS header so we can see the RFC 5929 (https://datatracker.ietf.org/doc/html/rfc5929)
            // application data ("tls-server-end-point:" + SHA-256 of the cert).
            var span = bytes.Span;
            var appLen = BitConverter.ToInt32(span.Slice(24, 4));
            var appOff = BitConverter.ToInt32(span.Slice(28, 4));
            var app = span.Slice(appOff, appLen);
            var asciiPrefix = System.Text.Encoding.ASCII.GetString(app[..Math.Min(21, app.Length)]);
            var hash = Convert.ToHexString(app[Math.Min(21, app.Length)..]);

            await context.Response.WriteAsync(
                $"""
                    ITlsChannelBindingFeature.GetChannelBindingBytes(Endpoint)
                    ----------------------------------------------------------
                    raw length      = {bytes.Length} bytes (SEC_CHANNEL_BINDINGS header + appdata)
                    appdata length  = {appLen}
                    appdata offset  = {appOff}
                    ASCII prefix    = "{asciiPrefix}"
                    cert hash (hex) = {hash}


                """);
        }
        else
        {
            await context.Response.WriteAsync("\tITlsChannelBindingFeature.GetChannelBindingBytes(Endpoint) returned null \n\n");
        }
    }

    await next(context);
});

// Example middleware using TryGetTlsClientHello API to query TLS Client Hello raw bytes.
app.Use(async (context, next) =>
{
    var connectionFeature = context.Features.GetRequiredFeature<IHttpConnectionFeature>();
    var httpSysPropFeature = context.Features.GetRequiredFeature<IHttpSysRequestPropertyFeature>();
    var tlsHandshakeFeature = context.Features.GetRequiredFeature<ITlsHandshakeFeature>();

    // first time invocation to find out required size
    var success = httpSysPropFeature.TryGetTlsClientHello(Array.Empty<byte>(), out var bytesReturned);
    Debug.Assert(!success);
    Debug.Assert(bytesReturned > 0);

    // rent with enough memory span and invoke
    var bytes = ArrayPool<byte>.Shared.Rent(bytesReturned);
    success = httpSysPropFeature.TryGetTlsClientHello(bytes, out _);
    Debug.Assert(success);

    await context.Response.WriteAsync(
        $"""
            TryGetTlsClientHello
            --------------------
            connectionId            = {connectionFeature.ConnectionId};
            negotiated cipher suite = {tlsHandshakeFeature.NegotiatedCipherSuite}; 
            tlsClientHello.length   = {bytesReturned};
            tlsclienthello start    = {string.Join(' ', bytes.AsSpan(0, 30).ToArray())}


        """);
        
    await next(context);
});

// Example middleware exercising the generic IHttpSysRequestPropertyFeature.TryGetRequestProperty API.
app.Use(async (context, next) =>
{
    // From Win SDK http.h
    const int HttpRequestPropertyTlsClientHello = 11;

    var httpSysPropFeature = context.Features.GetRequiredFeature<IHttpSysRequestPropertyFeature>();

    try
    {
        // probe required size with empty output buffer and empty qualifier
        var success = httpSysPropFeature.TryGetRequestProperty(
            HttpRequestPropertyTlsClientHello,
            qualifier: default,
            output: default,
            out var requiredSize);
        Debug.Assert(!success);
        Debug.Assert(requiredSize > 0);

        var rented = ArrayPool<byte>.Shared.Rent(requiredSize);
        try
        {
            success = httpSysPropFeature.TryGetRequestProperty(
                HttpRequestPropertyTlsClientHello,
                qualifier: default,
                output: rented.AsSpan(0, requiredSize),
                out var written);
            Debug.Assert(success);

            await context.Response.WriteAsync(
                $"""
                    TryGetRequestProperty(HttpRequestPropertyTlsClientHello)
                    --------------------------------------------------------
                    requiredSize     = {requiredSize}
                    bytesReturned    = {written}
                    first 30 bytes   = {string.Join(' ', rented.AsSpan(0, Math.Min(30, written)).ToArray())}


                """);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
    catch (Exception ex)
    {
        await context.Response.WriteAsync(
            $"""

                TryGetRequestProperty(HttpRequestPropertyTlsClientHello) threw: {ex.GetType().Name}: {ex.Message}
            """);
    }

    await next(context);
});

app.Run();
