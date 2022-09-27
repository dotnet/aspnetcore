// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Test.Internal;

namespace FunctionalTests;

public class Program
{
    public static Task Main(string[] args)
    {
        string url = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--url":
                    i += 1;
                    url = args[i];
                    break;
            }
        }

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureLogging(factory =>
                {
                    factory.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.TimestampFormat = "[HH:mm:ss] ";
                        options.UseUtcTimestamp = true;
                    });
                    factory.AddDebug();
                    factory.SetMinimumLevel(LogLevel.Debug);
                })
                .UseKestrel((builderContext, options) =>
                {
                    options.ConfigureHttpsDefaults(httpsOptions =>
                    {
                        httpsOptions.ServerCertificate = TestCertificateHelper.GetTestCert();
                    });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();

                if (!string.IsNullOrEmpty(url))
                {
                    Console.WriteLine($"Forcing URL to: {url}");
                    webHostBuilder.UseUrls(url);
                }
            });

        return hostBuilder.Build().RunAsync();
    }
}
