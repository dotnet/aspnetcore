using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.W3C;

namespace Logging.W3C.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(factory =>
                {
                    // Add W3CLogger with all fields enabled.
                    // By default, W3CLogger will use W3CLoggingFields.Default, which excludes
                    // UserName & Cookie.
                    factory.AddW3CLogger(logging =>
                    {
                        logging.LoggingFields = W3CLoggingFields.All;
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
