// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// The DirectTls public API is experimental.
#pragma warning disable ASPNETCORE_DIRECTTLS_001

// Set USE_STANDARD_TLS=1 to use standard Kestrel TLS (SslStream) for comparison.
var useStandardTls = Environment.GetEnvironmentVariable("USE_STANDARD_TLS") == "1";

var hostBuilder = new HostBuilder()
    .ConfigureLogging((_, factory) =>
    {
        factory.AddSimpleConsole();
        factory.SetMinimumLevel(LogLevel.Warning);
    })
    .ConfigureServices(services =>
    {
        services.AddRouting();
    })
    .ConfigureWebHost(webHost =>
    {
        if (!useStandardTls)
        {
            Console.WriteLine("Using DirectTls transport (native fd-bound OpenSSL TLS).");

            // Certificates for SNI-based selection: "p384.example" -> P-384 cert (serial 7BC8...),
            // anything else -> P-256 cert (serial 5810...).
            var p256 = X509Certificate2.CreateFromPemFile("server-p256.crt", "server-p256.key");
            var p384 = X509Certificate2.CreateFromPemFile("server-p384.crt", "server-p384.key");

            webHost.UseKestrel();
            // Register the DirectTls transport AFTER UseKestrel so it is offered DirectTlsEndpoints first.
            webHost.UseDirectTls();

            webHost.ConfigureKestrel(options =>
            {
                // Endpoint 5001 exercises every DirectTls feature at once:
                //   * ALPN / HTTP-2         -> ListenOptions.Protocols = Http1AndHttp2
                //   * per-endpoint SNI      -> Options.ServerCertificateSelector
                //   * ClientHello listener  -> Options.TlsClientHelloBytesCallback
                var demoEndpoint = new DirectTlsEndpoint(IPAddress.Any, 5001);

                // Per-endpoint certificate selection driven by the ClientHello SNI host. The connection is
                // already allocated so the selector can read its ConnectionContext (e.g. ConnectionId).
                demoEndpoint.Options.ServerCertificateSelector = (connection, host) =>
                    string.Equals(host, "p384.example", StringComparison.OrdinalIgnoreCase) ? p384 : p256;

                // Raw ClientHello inspection at handshake time. The callback receives the 5-byte TLS record
                // header + ClientHello handshake message: bytes[0] == 0x16 (handshake), bytes[5] == 0x01
                // (ClientHello). Inspect SNI / ALPN / cipher list here - do not block or throw.
                demoEndpoint.Options.TlsClientHelloBytesCallback = (connection, clientHelloBytes) =>
                {
                    var recordType = clientHelloBytes.IsEmpty ? (byte)0 : clientHelloBytes.FirstSpan[0];
                    Console.WriteLine(
                        $"[ClientHello] connection {connection.ConnectionId}: {clientHelloBytes.Length} bytes, " +
                        $"record type 0x{recordType:x2}");
                };
                // The HTTP protocols (ALPN) come from ListenOptions.Protocols, not the endpoint options.
                options.Listen(demoEndpoint, listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2);

                // "Clean" perf endpoint: DirectTls with a single fixed certificate and NO per-connection
                // callbacks - no ServerCertificateSelector (SNI) and no ClientHello listener. This isolates
                // the transport hot path from the observability/selection hooks to measure raw throughput.
                var perfEndpoint = new DirectTlsEndpoint(IPAddress.Any, 5002);
                perfEndpoint.Options.ServerCertificate = p256;
                options.Listen(perfEndpoint, listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2);
            });
        }
        else
        {
            Console.WriteLine("Using standard Kestrel TLS (SslStream)");

            webHost.UseKestrel(options =>
            {
                options.ListenAnyIP(5001, listenOptions =>
                {
                    listenOptions.UseHttps(X509CertificateLoader.LoadPkcs12FromFile("server-p256.pfx", "testpassword"));
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
                    // Echo the negotiated protocol so a demo request visibly shows HTTP/1.1 vs HTTP/2
                    // (e.g. curl --http2 https://p384.example:5001/ --resolve p384.example:5001:127.0.0.1 -k).
                    await context.Response.WriteAsync($"Hello world over {context.Request.Protocol}");
                });
            });
        });
    });

await hostBuilder.Build().RunAsync();
