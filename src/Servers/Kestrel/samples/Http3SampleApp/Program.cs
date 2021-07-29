// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Http3SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    factory.SetMinimumLevel(LogLevel.Trace);
                    factory.AddConsole();
                })
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseKestrel()
                    .ConfigureKestrel((context, options) =>
                    {
                        var cert = CertificateLoader.LoadFromStoreCert("localhost", StoreName.My.ToString(), StoreLocation.CurrentUser, false);
                        options.EnableAltSvc = true;
                        options.ConfigureHttpsDefaults(httpsOptions =>
                        {
                            httpsOptions.ServerCertificate = cert;
                        });

                        options.ListenAnyIP(5000, listenOptions =>
                        {
                            listenOptions.UseConnectionLogging();
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        });

                        options.ListenAnyIP(5001, listenOptions =>
                        {
                            listenOptions.UseHttps();
                            listenOptions.UseConnectionLogging();
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                        });

                        options.ListenAnyIP(5002, listenOptions =>
                        {
                            listenOptions.UseHttps(StoreName.My, "localhost");
                            listenOptions.UseConnectionLogging();
                            listenOptions.Protocols = HttpProtocols.Http3;
                        });

                        options.ListenAnyIP(5003, listenOptions =>
                        {
                            listenOptions.UseHttps(httpsOptions =>
                            {
                                httpsOptions.ServerCertificateSelector = (_, _) => cert;
                            });
                            listenOptions.UseConnectionLogging();
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2; // TODO: http3
                        });

                        options.ListenAnyIP(5004, listenOptions =>
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
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2; // TODO: http3
                        });
                    })
                    .UseStartup<Startup>();
                });

            hostBuilder.Build().Run();
        }
    }
}
