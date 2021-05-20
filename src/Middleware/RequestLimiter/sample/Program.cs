using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace RateLimiterSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
