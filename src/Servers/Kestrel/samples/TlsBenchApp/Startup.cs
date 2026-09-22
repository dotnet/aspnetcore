// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TlsBenchApp;

/// <summary>
/// Minimal HTTPS server for A/B-ing Kestrel's two TLS layers against each other in the same
/// binary, over the same transport, certificate and load. The response is the 13-byte
/// "Hello, World!" used by the plaintext benchmarks.
///
/// TLS_MODE=sansio   sets the sans-IO AppContext switch before Kestrel reads it
/// TLS_MODE=sslstream leaves it unset (the default SslStream path)
/// </summary>
public class Startup
{
    private const string SansIoSwitch = "Microsoft.AspNetCore.Server.Kestrel.EnableSansIoTls";

    private static readonly byte[] _helloWorldBytes = Encoding.UTF8.GetBytes("Hello, World!");

    public void Configure(IApplicationBuilder app)
    {
        app.Run((httpContext) =>
        {
            var response = httpContext.Response;

            // Guard against silently measuring the same TLS layer twice. The sans-IO path cannot
            // publish ISslStreamFeature, so its absence is a reliable discriminator: if the
            // capability probe or the platform allow-list rejected the layer, the switch is on but
            // the connection still ran on SslStream, and this reports that rather than hiding it.
            if (httpContext.Request.Path == "/tlsinfo")
            {
                var hasSslStream = httpContext.Features.Get<ISslStreamFeature>() is not null;
                var handshake = httpContext.Features.Get<ITlsHandshakeFeature>();
                var info = Encoding.UTF8.GetBytes(
                    $"layer={(hasSslStream ? "sslstream" : "sansio")} protocol={handshake?.Protocol} suite={handshake?.NegotiatedCipherSuite}");

                response.StatusCode = 200;
                response.ContentType = "text/plain";
                response.ContentLength = info.Length;
                return response.BodyWriter.WriteAsync(info).GetAsTask();
            }

            var payload = _helloWorldBytes;

            response.StatusCode = 200;
            response.ContentType = "text/plain";
            response.ContentLength = payload.Length;

            return response.BodyWriter.WriteAsync(payload).GetAsTask();
        });
    }

    public static async Task Main(string[] args)
    {
        var mode = Environment.GetEnvironmentVariable("TLS_MODE") ?? "sslstream";
        var port = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "5001", System.Globalization.CultureInfo.InvariantCulture);
        var certPath = Environment.GetEnvironmentVariable("CERT_PATH")
            ?? Path.Combine(AppContext.BaseDirectory, "testCert.pfx");

        // Must happen before the first connection is accepted: the middleware reads this switch
        // per connection to decide which TLS layer to build.
        var sansIo = string.Equals(mode, "sansio", StringComparison.OrdinalIgnoreCase);
        AppContext.SetSwitch(SansIoSwitch, sansIo);

        var certificate = new X509Certificate2(certPath, "testPassword");

        Console.WriteLine($"TLS_MODE={mode} sansIoSwitch={sansIo} port={port} cert={certificate.SubjectName.Name}");

        var host = new HostBuilder()
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, port, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                            listenOptions.UseHttps(certificate);
                        });
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
            })
            .Build();

        await host.RunAsync();
    }
}

internal static class ValueTaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task GetAsTask(this in ValueTask<FlushResult> valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            valueTask.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        return valueTask.AsTask();
    }
}
