// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Hosting;
using TlsFeaturesObserve.HttpSys;

namespace TlsFeatureObserve;

public static class Program
{
    public static void Main(string[] args)
    {
        HttpSysConfigurator.ConfigureCacheTlsClientHello();
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
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
                });
            });
}
