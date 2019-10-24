using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;

namespace Http3SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cert = CertificateLoader.LoadFromStoreCert("localhost", StoreName.My.ToString(), StoreLocation.CurrentUser, true);
            var hostBuilder = new WebHostBuilder()
                 .ConfigureLogging((_, factory) =>
                 {
                     // Set logging to the MAX.
                     factory.SetMinimumLevel(LogLevel.Trace);
                     factory.AddConsole();
                 })
                 .UseKestrel()
                 .UseMsQuic(options =>
                 {
                     options.Certificate = cert;
                     options.RegistrationName = "Quic";
                     options.Alpn = "h3-23";
                     options.IdleTimeout = TimeSpan.FromHours(1);
                 })
                 .ConfigureKestrel((context, options) =>
                 {
                     var basePort = 5555;

                     options.Listen(IPAddress.Any, basePort, listenOptions =>
                     {
                         listenOptions.UseHttps();
                     });

                     options.Listen(IPAddress.Any, basePort, listenOptions =>
                     {
                         listenOptions.UseHttps();
                         listenOptions.Protocols = HttpProtocols.Http3;
                     });
                 })
                 .UseContentRoot(Directory.GetCurrentDirectory())
                 .UseStartup<Startup>();

            hostBuilder.Build().Run();
        }
    }
}
