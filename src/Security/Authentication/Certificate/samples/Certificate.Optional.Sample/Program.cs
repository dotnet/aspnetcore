// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Certificate.Optional.Sample;

public class Program
{
    public const string HostWithoutCert = "127.0.0.1";
    public const string HostWithCert = "127.0.0.2";

    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // load the self-signed certificate issued for 127.0.0.1 and 127.0.0.2 domains
                // https://learn.microsoft.com/dotnet/core/additional-tools/self-signed-certificates-guide
                var serverCertificate = CertificateLoader.LoadFromStoreCert(
                    "localhost", "My", StoreLocation.CurrentUser,
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

                                // require a client certificate to access 127.0.0.2
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
