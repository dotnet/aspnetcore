using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CookiePolicySample
{
    public static class Program
    {
        public static Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseIISIntegration()
                        .UseStartup<Startup>();
                })
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Microsoft", LogLevel.Trace);
                })
                .Build();

            return host.RunAsync();
        }
    }
}
