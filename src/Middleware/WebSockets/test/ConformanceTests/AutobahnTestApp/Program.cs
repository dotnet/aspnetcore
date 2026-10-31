// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace AutobahnTestApp;

public class Program
{
    public static Task Main(string[] args)
    {
        var scenarioName = "Unknown";
        var config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureLogging(loggingBuilder => loggingBuilder.AddConsole())
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();

                if (string.Equals(webHostBuilder.GetSetting("server"), "Microsoft.AspNetCore.Server.HttpSys", System.StringComparison.Ordinal))
                {
                    scenarioName = "HttpSysServer";
                    Console.WriteLine("Using HttpSys server");
                    webHostBuilder.UseHttpSys();
                }
                else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_PORT")))
                {
                    // ANCM is hosting the process.
                    // The port will not yet be configured at this point, but will also not require HTTPS.
                    scenarioName = "AspNetCoreModule";
                    Console.WriteLine("Detected ANCM, using Kestrel");
                    webHostBuilder.UseKestrel();
                }
                else
                {
                    // Also check "server.urls" for back-compat.
                    var urls = webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey) ?? webHostBuilder.GetSetting("server.urls");
                    webHostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Empty);

                    Console.WriteLine($"Using Kestrel, URL: {urls}");

                    if (urls.Contains(";"))
                    {
                        throw new NotSupportedException("This test app does not support multiple endpoints.");
                    }

                    var uri = new Uri(urls);

                    webHostBuilder.UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, uri.Port, listenOptions =>
                        {
                            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                            {
                                scenarioName = "Kestrel(SSL)";
                                var certPath = Path.Combine(AppContext.BaseDirectory, "TestResources", "testCert.pfx");
                                Console.WriteLine($"Using SSL with certificate: {certPath}");
                                listenOptions.UseHttps(certPath, "testPassword");
                            }
                            else
                            {
                                scenarioName = "Kestrel(NonSSL)";
                            }
                        });
                    });
                }
            });

        var host = builder.Build();

        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
        {
            Console.WriteLine($"Unhandled exception (Scenario: {scenarioName}): {a.ExceptionObject.ToString()}");
        };

        Console.WriteLine($"Starting Server for Scenario: {scenarioName}");
        return host.RunAsync();
    }
}
