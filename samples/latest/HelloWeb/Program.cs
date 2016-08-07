using Microsoft.AspNetCore.Hosting;

namespace HelloWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseStartup<Startup>()
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Build();

            host.Run();
        }
    }
}
