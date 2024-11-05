// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Http2SampleApp;

public class Program
{
    public static void Main(string[] args)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .ConfigureKestrel((context, options) =>
                    {
                        var basePort = context.Configuration.GetValue<int?>("BASE_PORT") ?? 5000;

                        // Http/1.1 endpoint for comparison
                        options.ListenAnyIP(basePort, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });

                        // TLS Http/1.1 or HTTP/2 endpoint negotiated via ALPN
                        options.ListenAnyIP(basePort + 1, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                            listenOptions.UseHttps();
                            listenOptions.Use((context, next) =>
                            {
                                // https://tools.ietf.org/html/rfc7540#appendix-A
                                // Allows filtering TLS handshakes on a per connection basis

                                var tlsFeature = context.Features.Get<ITlsHandshakeFeature>();

                                if (tlsFeature.CipherAlgorithm == CipherAlgorithmType.Null)
                                {
                                    throw new NotSupportedException("Prohibited cipher: " + tlsFeature.CipherAlgorithm);
                                }

                                return next(context);
                            });
                        });

                        // Prior knowledge, no TLS handshake. WARNING: Not supported by browsers
                        // but useful for the h2spec tests
                        options.ListenAnyIP(basePort + 5, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
            })
            .ConfigureLogging((_, factory) =>
            {
                // Set logging to the MAX.
                factory.SetMinimumLevel(LogLevel.Trace);
                factory.AddConsole();
            });

        Console.WriteLine($"Process ID: {Environment.ProcessId}");

        hostBuilder.Build().Run();
    }
}
