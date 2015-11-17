using Microsoft.AspNet.Builder;
using Microsoft.Extensions.Logging;

namespace HelloWeb
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseStaticFiles();
            app.UseWelcomePage();
        }
    }
}