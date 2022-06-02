// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Http3SampleApp;

public class Program
{
    public static void Main(string[] args)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureLogging((_, factory) =>
            {
                factory.SetMinimumLevel(LogLevel.Trace);
                factory.AddSimpleConsole(o => o.TimestampFormat = "[HH:mm:ss.fff] ");
            })
            .ConfigureWebHost(webHost =>
            {
                var cert = CertificateLoader.LoadFromStoreCert("localhost", StoreName.My.ToString(), StoreLocation.CurrentUser, false);

                webHost.UseKestrel()
                .ConfigureKestrel((context, options) =>
                {
                    options.ListenAnyIP(5000, listenOptions =>
                    {
                        listenOptions.UseConnectionLogging();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });

                    options.Listen(IPAddress.Any, 5001, listenOptions =>
                    {
                        listenOptions.UseHttps();
                        listenOptions.UseConnectionLogging();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    });

                    options.ListenAnyIP(5002, listenOptions =>
                    {
                        listenOptions.UseConnectionLogging();
                        listenOptions.UseHttps(StoreName.My, "localhost");
                        listenOptions.Protocols = HttpProtocols.Http3;
                    });

                    options.ListenAnyIP(5003, listenOptions =>
                    {
                        listenOptions.UseHttps(httpsOptions =>
                        {
                            // ConnectionContext is null
                            httpsOptions.ServerCertificateSelector = (context, host) => cert;
                        });
                        listenOptions.UseConnectionLogging();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    });

                    // No SslServerAuthenticationOptions callback is currently supported by QuicListener
                    options.ListenAnyIP(5004, listenOptions =>
                    {
                        listenOptions.UseHttps(httpsOptions =>
                        {
                            httpsOptions.OnAuthenticate = (_, sslOptions) => sslOptions.ServerCertificate = cert;
                        });
                        listenOptions.UseConnectionLogging();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });

                    // ServerOptionsSelectionCallback isn't currently supported by QuicListener
                    options.ListenAnyIP(5005, listenOptions =>
                    {
                        ServerOptionsSelectionCallback callback = (SslStream stream, SslClientHelloInfo clientHelloInfo, object state, CancellationToken cancellationToken) =>
                        {
                            var options = new SslServerAuthenticationOptions()
                            {
                                ServerCertificate = cert,
                            };
                            return new ValueTask<SslServerAuthenticationOptions>(options);
                        };
                        listenOptions.UseHttps(callback, state: null);
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });

                    // TlsHandshakeCallbackOptions (ServerOptionsSelectionCallback) isn't currently supported by QuicListener
                    options.ListenAnyIP(5006, listenOptions =>
                    {
                        listenOptions.UseHttps(new TlsHandshakeCallbackOptions()
                        {
                            OnConnection = context =>
                            {
                                var options = new SslServerAuthenticationOptions()
                                {
                                    ServerCertificate = cert,
                                };
                                return new ValueTask<SslServerAuthenticationOptions>(options);
                            },
                        });
                        listenOptions.UseConnectionLogging();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });

                    // Port configured for WebTransport
                    options.Listen(IPAddress.Any, 5007, listenOptions =>
                    {
                        AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", true);
                        listenOptions.UseHttps(GenerateManualCertificate());
                        listenOptions.UseConnectionLogging();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    });
                })
                .UseStartup<Startup>();
            });

        var host = hostBuilder.Build();

        // Listener needs to be configured before host (and HTTP/3 endpoints) start up.
        using var httpEventSource = new HttpEventSourceListener(host.Services.GetRequiredService<ILoggerFactory>());

        host.Run();
    }

    // Adapted from: https://github.com/wegylexy/webtransport
    // We will need to eventually merge this with existing Kestrel certificate generation
    // tracked in issue #41762
    private static X509Certificate2 GenerateManualCertificate()
    {
        X509Certificate2 cert = null;
        var store = new X509Store("KestrelWebTransportCertificates", StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        if (store.Certificates.Count > 0)
        {
            cert = store.Certificates[^1];

            // rotate key after it expires
            if (DateTime.Parse(cert.GetExpirationDateString(), null) < DateTimeOffset.UtcNow)
            {
                cert = null;
            }
        }
        if (cert == null)
        {
            // generate a new cert
            var now = DateTimeOffset.UtcNow;
            SubjectAlternativeNameBuilder sanBuilder = new();
            sanBuilder.AddDnsName("localhost");
            using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest req = new("CN=localhost", ec, HashAlgorithmName.SHA256);
            // Adds purpose
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
            {
                new("1.3.6.1.5.5.7.3.1") // serverAuth
            }, false));
            // Adds usage
            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
            // Adds subject alternate names
            req.CertificateExtensions.Add(sanBuilder.Build());
            // Sign
            using var crt = req.CreateSelfSigned(now, now.AddDays(14)); // 14 days is the max duration of a certificate for this
            cert = new(crt.Export(X509ContentType.Pfx));

            // Save
            store.Add(cert);
        }
        store.Close();

        var hash = SHA256.HashData(cert.RawData);
        var certStr = Convert.ToBase64String(hash);
        Console.WriteLine($"\n\n\n\n\nCertificate: {certStr}\n\n\n\n"); // <-- you will need to put this output into the JS API call to allo wthe connection
        return cert;
    }
}
