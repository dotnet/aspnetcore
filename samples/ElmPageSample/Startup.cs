using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace ElmPageSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddElm();

            services.ConfigureElm(elmOptions =>
            {
                elmOptions.Filter = (loggerName, loglevel) => loglevel == LogLevel.Verbose;
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseElmPage();

            app.UseElmCapture();

            app.UseMiddleware<HelloWorldMiddleware>();
        }
    }
}
