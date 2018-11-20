using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace GenericWebHost
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddCommandLine(args);
                })
                .UseFakeServer()
                .ConfigureWebHost(builder =>
                {
                    builder.Configure(app =>
                    {
                        app.Run(async (context) =>
                        {
                            await context.Response.WriteAsync("Hello World!");
                        });
                    });
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}
