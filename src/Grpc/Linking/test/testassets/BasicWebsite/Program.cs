// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Console;

namespace BasicWebsite;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(builder =>
            {
                builder.AddSimpleConsole(loggerOptions =>
                {
                    loggerOptions.ColorBehavior = LoggerColorBehavior.Disabled;
                });
                builder.SetMinimumLevel(LogLevel.Trace);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((context, options) =>
                {
                    // Support --port and --use_tls cmdline arguments normally supported
                    // by gRPC interop servers.
                    var useTls = Convert.ToBoolean(context.Configuration["use_tls"], CultureInfo.InvariantCulture);

                    options.Limits.MinRequestBodyDataRate = null;
                    options.ListenAnyIP(0, listenOptions =>
                    {
                        Console.WriteLine($"Enabling connection encryption: {useTls}");

                        if (useTls)
                        {
                            var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                            var certPath = Path.Combine(basePath!, "Certs", "server1.pfx");

                            listenOptions.UseHttps(certPath, "1111");
                        }
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                });
                webBuilder.UseStartup<Startup>();
            });
}
