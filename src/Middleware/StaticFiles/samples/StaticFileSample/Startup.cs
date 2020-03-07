using System;
using System.IO;
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
            services.AddResponseCompression();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment host)
        {
            app.UseResponseCompression();

            app.UseFileServer(new FileServerOptions
            {
                EnableDirectoryBrowsing = true
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .ConfigureLogging(factory =>
                {
                    factory.AddFilter("Console", level => level >= LogLevel.Debug);
                    factory.AddConsole();
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                // .UseHttpSys()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
