using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace HelloMvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseStartup<Startup>()
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseApplicationBasePath(Directory.GetCurrentDirectory())
                .Build();

            host.Run();
        }
    }
}