using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace ComponentsWebSite
{
    public class Program
    {
        public static int Main(string [] args)
        {
            var host = BuildWebHost(args);

            host.Run();
            return 0;
        }

        public static IWebHost BuildWebHost(string[] args) =>
            CreateWebHostBuilder().Build();

        public static IWebHostBuilder CreateWebHostBuilder()
        {
            return new WebHostBuilder()
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel();
        }
    }
}
