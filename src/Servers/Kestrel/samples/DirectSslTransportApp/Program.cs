// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPNETCORE_DIRECTSSL_001 // Experimental DirectSsl API

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Use environment variable to switch between DirectSsl and standard TLS
// Set USE_STANDARD_TLS=1 to use standard Kestrel TLS (SslStream)
var useStandardTls = Environment.GetEnvironmentVariable("USE_STANDARD_TLS") == "1";
var logFilePath = "directssl.log";

// Clear log file on startup
File.WriteAllText(logFilePath, $"=== DirectSslTransportApp started at {DateTime.Now} (StandardTLS={useStandardTls}) ===\n");

// Add global exception handlers to catch crashes
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var msg = $"[FATAL] UnhandledException: {e.ExceptionObject}\n";
    Console.Error.WriteLine(msg);
    File.AppendAllText(logFilePath, msg);
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    var msg = $"[FATAL] UnobservedTaskException: {e.Exception}\n";
    Console.Error.WriteLine(msg);
    File.AppendAllText(logFilePath, msg);
    e.SetObserved();
};

var hostBuilder = new HostBuilder()
    .ConfigureLogging((_, factory) =>
    {
        factory.SetMinimumLevel(useStandardTls ? LogLevel.Warning : LogLevel.None);
    })
    .ConfigureServices(services =>
    {
        services.AddRouting();
    })
    .ConfigureWebHost(webHost =>
    {
        if (!useStandardTls)
        {
            Console.WriteLine("Using DirectSsl transport (OpenSSL)");

            // Configure Kestrel to use the Direct Socket Transport
            webHost.UseKestrelDirectSslTransport();

            webHost.UseDirectSslSockets(options =>
            {
                options.CertificatePath = "server-p384.crt";
                options.PrivateKeyPath = "server-p384.key";
                options.WorkerCount = 4;
            });

            webHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });
            });
        }
        else
        {
            Console.WriteLine("Using standard Kestrel TLS (SslStream)");

            webHost.UseKestrel(options =>
            {
                options.ListenAnyIP(5001, listenOptions =>
                {
                    listenOptions.UseHttps(new X509Certificate2("server-p384.pfx", "testpassword"));
                    listenOptions.Protocols = HttpProtocols.Http1;
                });
            });
        }

        webHost.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello world");
                });
            });
        });
    });

await hostBuilder.Build().RunAsync();
