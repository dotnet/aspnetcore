using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TestServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) => BuildWebHost<Startup>(args);

        public static IWebHost BuildWebHost<TStartup>(string[] args) where TStartup : class =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                        .AddCommandLine(args)
                        .Build())
                .UseStartup<TStartup>()
                .UseStaticWebAssets()
                .Build();
    }
}
