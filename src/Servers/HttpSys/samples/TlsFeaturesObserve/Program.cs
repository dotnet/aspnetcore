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

                options.Authentication.Schemes = AuthenticationSchemes.None;
                options.Authentication.AllowAnonymous = true;

                var property = typeof(HttpSysOptions).GetProperty("TlsClientHelloBytesCallback", BindingFlags.NonPublic | BindingFlags.Instance);
                var delegateType = property.PropertyType; // Get the exact delegate type

                // Create a delegate of the correct type
                var callbackDelegate = Delegate.CreateDelegate(delegateType, typeof(Holder).GetMethod(nameof(Holder.ProcessTlsClientHello), BindingFlags.Static | BindingFlags.Public));

                property?.SetValue(options, callbackDelegate);
            });
        });

public static class Holder
{
    public static void ProcessTlsClientHello(IFeatureCollection features, ReadOnlySpan<byte> tlsClientHelloBytes)
    {
        var httpConnectionFeature = features.Get<IHttpConnectionFeature>();

        var myTlsFeature = new MyTlsFeature(
            connectionId: httpConnectionFeature.ConnectionId,
            tlsClientHelloLength: tlsClientHelloBytes.Length);

        features.Set<IMyTlsFeature>(myTlsFeature);
    }
}

    // rent with enough memory span and invoke
    var bytes = ArrayPool<byte>.Shared.Rent(bytesReturned);
    success = httpSysPropFeature.TryGetTlsClientHello(bytes, out _);
    Debug.Assert(success);

    await context.Response.WriteAsync($"[Response] connectionId={connectionFeature.ConnectionId}; tlsClientHello.length={bytesReturned}; tlsclienthello start={string.Join(' ', bytes.AsSpan(0, 30).ToArray())}");
    await next(context);
});

app.Run();
