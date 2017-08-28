#if NET461
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class ServiceBaseControlled
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<IHostedService, MyServiceA>();
                    services.AddScoped<IHostedService, MyServiceB>();
                });

            await builder.RunAsServiceAsync();
        }
    }
}
#endif