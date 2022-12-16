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
                })
                .UseStartup<Startup>();
            });

        var host = hostBuilder.Build();

        // Listener needs to be configured before host (and HTTP/3 endpoints) start up.
        using var httpEventSource = new HttpEventSourceListener(host.Services.GetRequiredService<ILoggerFactory>());

        host.Run();
    }
}
