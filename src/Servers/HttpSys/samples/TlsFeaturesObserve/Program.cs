// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Hosting;
using TlsFeatureObserve;
using TlsFeaturesObserve.HttpSys;

HttpSysConfigurator.ConfigureCacheTlsClientHello();
CreateHostBuilder(args).Build().Run();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseStartup<Startup>()
            .UseHttpSys(options =>
            {
                // If you want to use https locally: https://stackoverflow.com/a/51841893
                options.UrlPrefixes.Add("https://*:6000"); // HTTPS

                options.Authentication.Schemes = AuthenticationSchemes.None;
                options.Authentication.AllowAnonymous = true;

                // If you want to resolve a callback API, uncomment.
                // Recommended approach is to use the on-demand API to fetch TLS client hello bytes,
                // look into Startup.cs for details.

                //var property = typeof(HttpSysOptions).GetProperty("TlsClientHelloBytesCallback", BindingFlags.NonPublic | BindingFlags.Instance);
                //var delegateType = property.PropertyType; // Get the exact delegate type

                //// Create a delegate of the correct type
                //var callbackDelegate = Delegate.CreateDelegate(delegateType, typeof(Holder).GetMethod(nameof(Holder.ProcessTlsClientHello), BindingFlags.Static | BindingFlags.Public));

                //property?.SetValue(options, callbackDelegate);
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

public interface IMyTlsFeature
{
    string ConnectionId { get; }
    int TlsClientHelloLength { get; }
}

public class MyTlsFeature : IMyTlsFeature
{
    public string ConnectionId { get; }
    public int TlsClientHelloLength { get; }

    public MyTlsFeature(string connectionId, int tlsClientHelloLength)
    {
        ConnectionId = connectionId;
        TlsClientHelloLength = tlsClientHelloLength;
    }
}
