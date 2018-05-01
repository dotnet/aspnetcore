using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class ProgramExternallyControlled
    {
        private IHost _host;

        public ProgramExternallyControlled()
        {
            _host = new HostBuilder()
                .UseServiceProviderFactory<MyContainer>(new MyContainerFactory())
                .ConfigureContainer<MyContainer>((hostContext, container) =>
                {
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json", optional: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MyServiceA>();
                    services.AddHostedService<MyServiceB>();
                })
                .Build();
        }

        public void Start()
        {
            _host.Start();
        }

        public async Task StopAsync()
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
    }
}
