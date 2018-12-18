using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace OpenIdConnectSample
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                    factory.AddDebug();
                    factory.AddFilter("Console", level => level >= LogLevel.Information);
                    factory.AddFilter("Debug", level => level >= LogLevel.Information);
                })
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 44318, listenOptions =>
                    {
                        // Configure SSL
                        var serverCertificate = LoadCertificate();
                        listenOptions.UseHttps(serverCertificate);
                    });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }

        private static X509Certificate2 LoadCertificate()
        {
            var assembly = typeof(Startup).GetTypeInfo().Assembly;
            var embeddedFileProvider = new EmbeddedFileProvider(assembly, "OpenIdConnectSample");
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
}
