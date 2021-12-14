// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;

namespace Certificate.Optional.Sample;

public class Program
{
    public const string HostWithoutCert = "example.com";
    public const string HostWithCert = "cert.example.com";

    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // the certificate is issued for example.com and cert.example.com domains
                var serverCertificate = CertificateLoader.LoadFromStoreCert(
                    "example.com", "My", StoreLocation.CurrentUser,
                    allowInvalid: true);

                webBuilder.UseStartup<Startup>();
                webBuilder.ConfigureKestrel((context, options) =>
                {
                    options.ListenAnyIP(5001, listenOptions =>
                    {
                        listenOptions.UseHttps(new TlsHandshakeCallbackOptions()
                        {
                            OnConnection = connectionContext =>
                            {
                                // allow the tls connection without a client certificate
                                if (connectionContext.ClientHelloInfo.ServerName.Equals(HostWithoutCert, StringComparison.OrdinalIgnoreCase))
                                {
                                    return new ValueTask<SslServerAuthenticationOptions>(new SslServerAuthenticationOptions()
                                    {
                                        ServerCertificate = serverCertificate,
                                        ClientCertificateRequired = false
                                    });
                                }

                                // require a client certificate to access cert.example.com
                                return new ValueTask<SslServerAuthenticationOptions>(new SslServerAuthenticationOptions()
                                {
                                    ClientCertificateRequired = true,
                                    ServerCertificate = serverCertificate,
                                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => certificate is not null
                                });
                            }
                        });
                    });
                });
            });
}
