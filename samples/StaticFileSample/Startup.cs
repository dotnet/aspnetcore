using Microsoft.AspNet.Builder;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;

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
            factory.AddConsole(LogLevel.Verbose);

            app.UseRequestServices();

            app.UseFileServer(new FileServerOptions()
            {
                EnableDirectoryBrowsing = true,
            });
        }
    }
}