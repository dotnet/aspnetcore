// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.FileProviders;

namespace WsFedSample;

public class Program
{
    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 44307, listenOptions =>
                        {
                            // Configure SSL
                            var serverCertificate = LoadCertificate();
                            listenOptions.UseHttps(serverCertificate);
                        });
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>();
            })
            .ConfigureLogging(factory =>
            {
                factory.AddConsole();
                factory.AddDebug();
                factory.AddFilter("Console", level => level >= LogLevel.Information);
                factory.AddFilter("Debug", level => level >= LogLevel.Information);
            })
            .Build();

        return host.RunAsync();
    }

    private static X509Certificate2 LoadCertificate()
    {
        var assembly = typeof(Startup).Assembly;
        var embeddedFileProvider = new EmbeddedFileProvider(assembly, "WsFedSample");
        var certificateFileInfo = embeddedFileProvider.GetFileInfo("compiler/resources/cert.pfx");
        using (var certificateStream = certificateFileInfo.CreateReadStream())
        {
            byte[] certificatePayload;
            using (var memoryStream = new MemoryStream())
            {
                certificateStream.CopyTo(memoryStream);
                certificatePayload = memoryStream.ToArray();
            }

            return new X509Certificate2(certificatePayload, "testPassword");
        }
    }
}
