using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthChecksSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            new WebHostBuilder()
                .ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                })
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
    }
}
