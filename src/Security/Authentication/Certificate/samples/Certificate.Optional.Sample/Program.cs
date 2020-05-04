using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;

namespace Certificate.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        // Kestrel can't have different ssl settings on the same IP because there's no way to change them based on SNI.
                        // https://github.com/dotnet/runtime/issues/31097
                        // TODO: Provide a certificate that allows 127.0.0.1 as a host so we don't have to worry about setting up DNS, host files, etc..
                        options.Listen(IPAddress.Parse("127.0.0.1"), 5001, listenOptions =>
                        {
                            listenOptions.UseHttps(httpsOptions =>
                            {
                                httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                            });
                        });
                        options.Listen(IPAddress.Parse("127.0.0.2"), 5001, listenOptions =>
                        {
                            listenOptions.UseHttps(httpsOptions =>
                            {
                                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                            });
                        });
                    });
                });
    }
}
