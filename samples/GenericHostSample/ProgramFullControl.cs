using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class ProgramFullControl
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .UseServiceProviderFactory<MyContainer>(new MyContainerFactory())
                .ConfigureContainer<MyContainer>((hostContext, container) =>
                {
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MyServiceA>();
                    services.AddHostedService<MyServiceB>();
                })
                .Build();

            var s = host.Services;

            using (host)
            {
                Console.WriteLine("Starting!");

                await host.StartAsync();

                Console.WriteLine("Started! Press <enter> to stop.");

                Console.ReadLine();

                Console.WriteLine("Stopping!");

                await host.StopAsync();

                Console.WriteLine("Stopped!");
            }
        }
    }
}
