using System.Buffers;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;

namespace QuicSampleApp
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
                     options.Alpn = "HTTP/0.9";
                     options.RegistrationName = "ASP.NET Core Registration";
                 })
                 .ConfigureKestrel((context, options) =>
                 {

                     options.Listen(IPAddress.Any, 5555, listenOptions =>
                     {
                         listenOptions.UseHttps();
                         listenOptions.Run(async (ctx) =>
                         {
                             while (true)
                             {
                                 var readResult = await ctx.Transport.Input.ReadAsync();
                                 if (readResult.IsCompleted)
                                 {
                                     return;
                                 }
                                 await ctx.Transport.Output.WriteAsync(readResult.Buffer.ToArray());
                             }
                         });
                     });
     
                 })
                 .UseStartup<Startup>();

            hostBuilder.Build().Run();
        }

        public class Startup
        {
            public void Configure(IApplicationBuilder app)
            {
            }
        }
    }
}
