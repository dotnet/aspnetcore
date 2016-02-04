using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StaticFilesSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDirectoryBrowser();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory factory)
        {
            // Displays all log levels
            factory.AddConsole(LogLevel.Debug);

            app.UseFileServer(new FileServerOptions
            {
                EnableDirectoryBrowsing = true
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}