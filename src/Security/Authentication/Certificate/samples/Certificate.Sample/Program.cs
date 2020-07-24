using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;

namespace Certificate.Sample
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseStartup<Startup>()
                        .ConfigureKestrel(options =>
                        {
                            options.ConfigureHttpsDefaults(opt =>
                            {
                                opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                            });
                        });
                })
                .Build();

            return host.RunAsync();
        }
    }
}
