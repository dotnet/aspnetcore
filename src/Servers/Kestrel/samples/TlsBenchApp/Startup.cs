// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
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

            // RSS does not resolve per-connection differences - the earlier investigation saw the
            // same 5,000-connection point vary by 20 KB/connection across identical runs. Managed
            // heap size after a forced collection, plus cumulative allocation, is an in-process
            // counter that does resolve. ?gc=1 collects first; leave it off during a load run so
            // the measurement does not perturb what it measures.
            if (httpContext.Request.Path == "/gcinfo")
            {
                if (httpContext.Request.Query.ContainsKey("gc"))
                {
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                }

                var info = Encoding.UTF8.GetBytes(
                    $"allocated={GC.GetTotalAllocatedBytes(precise: false)} "
                    + $"heap={GC.GetGCMemoryInfo().HeapSizeBytes} "
                    + $"workingset={Environment.WorkingSet} "
                    + $"gen0={GC.CollectionCount(0)} gen1={GC.CollectionCount(1)} gen2={GC.CollectionCount(2)}");

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
        // Idle-connection client: opens N TLS connections, does one request on each, then holds
        // them open. Used to measure per-connection footprint against a server whose managed heap
        // can be read from /gcinfo.
        if (args.Length > 0 && args[0] == "holdconnections")
        {
            await HoldConnectionsAsync(args);
            return;
        }

        var mode = Environment.GetEnvironmentVariable("TLS_MODE") ?? "sslstream";
        var port = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "5001", System.Globalization.CultureInfo.InvariantCulture);
        var certPath = Environment.GetEnvironmentVariable("CERT_PATH")
            ?? Path.Combine(AppContext.BaseDirectory, "testCert.pfx");

        // Must happen before the first connection is accepted: the middleware reads this switch
        // per connection to decide which TLS layer to build.
        var sansIo = string.Equals(mode, "sansio", StringComparison.OrdinalIgnoreCase);
        var proto = string.Equals(mode, "proto", StringComparison.OrdinalIgnoreCase);
        AppContext.SetSwitch(SansIoSwitch, sansIo);

        // Every lab profile drives load from a separate machine. Binding loopback-only there
        // turns every response into a connection failure and the run still reports an rps.
        var bindAny = string.Equals(
            Environment.GetEnvironmentVariable("SERVER_BIND"), "any", StringComparison.OrdinalIgnoreCase);
        var bindAddress = bindAny ? IPAddress.Any : IPAddress.Loopback;

        var certificate = new X509Certificate2(certPath, "testPassword");

        Console.WriteLine($"TLS_MODE={mode} sansIoSwitch={sansIo} bind={bindAddress} port={port} cert={certificate.SubjectName.Name}");

        var host = new HostBuilder()
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(options =>
                    {
                        options.Listen(bindAddress, port, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;

                            if (proto)
                            {
                                // The standalone prototype adapter, swapped in as connection
                                // middleware exactly as the original PoC server did. Kestrel's
                                // own HTTPS middleware is bypassed entirely, so this measures
                                // the adapter without the integrated path around it.
                                var serverOptions = new SslServerAuthenticationOptions
                                {
                                    ServerCertificate = certificate,
                                    EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                                };
                                var tlsContext = TlsContext.CreateServer(serverOptions);

                                listenOptions.Use(next => async connection =>
                                {
                                    var tls = new ProtoTlsSessionDuplexPipe(connection.Transport);
                                    var original = connection.Transport;

                                    try
                                    {
                                        await tls.HandshakeAsync(tlsContext);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Error.WriteLine($"[proto] handshake failed: {ex.GetType().Name}: {ex.Message}");
                                        connection.Abort();
                                        return;
                                    }

                                    connection.Transport = tls;

                                    try
                                    {
                                        await next(connection);
                                    }
                                    finally
                                    {
                                        connection.Transport = original;
                                        await tls.DisposeAsync();
                                    }
                                });
                            }
                            else
                            {
                                listenOptions.UseHttps(certificate);
                            }
                        });
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
            })
            .Build();

        await host.StartAsync();

        // Report the layer that actually served a request before any load arrives. A harness
        // reads this to confirm the two scenarios really differ; the switch being on does not
        // mean the sans-IO layer ran, because the platform allow-list or the capability probe
        // can decline it and fall back to SslStream without saying so.
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            };
            using var client = new HttpClient(handler);
            Console.WriteLine($"selfcheck {await client.GetStringAsync($"https://127.0.0.1:{port}/tlsinfo")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"selfcheck failed: {ex.Message}");
        }

        // Logging is pinned at Warning so the benchmark does not measure the logger, which
        // means the host's own "Application started" line is never emitted. Harnesses wait on
        // this marker, so print it explicitly and only once the server is actually serving.
        Console.WriteLine("Application started.");

        await host.WaitForShutdownAsync();
    }

    /// <summary>
    /// Opens <c>count</c> TLS connections, issues one request on each so the handshake and the
    /// first read/write have both happened, then holds them idle. The point is what the server
    /// still holds per connection once nothing is in flight - the adapter returns its pooled
    /// buffers when idle, so a connection that has served a request and gone quiet is the case
    /// worth measuring.
    /// </summary>
    private static async Task HoldConnectionsAsync(string[] args)
    {
        var count = args.Length > 1 ? int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 1000;
        var port = args.Length > 2 ? int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture) : 5099;
        var holdSeconds = args.Length > 3 ? int.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture) : 30;

        var connections = new List<SslStream>(count);
        var request = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n");
        var readBuffer = new byte[4096];

        for (var i = 0; i < count; i++)
        {
            var socket = new TcpClient();
            await socket.ConnectAsync(IPAddress.Loopback, port);

            var ssl = new SslStream(socket.GetStream(), leaveInnerStreamOpen: false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
            });

            await ssl.WriteAsync(request);
            await ssl.ReadAsync(readBuffer);

            connections.Add(ssl);
        }

        Console.WriteLine($"held={connections.Count}");
        await Task.Delay(TimeSpan.FromSeconds(holdSeconds));

        foreach (var c in connections)
        {
            c.Dispose();
        }
    }
}

internal static class ValueTaskExtensions
{    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
