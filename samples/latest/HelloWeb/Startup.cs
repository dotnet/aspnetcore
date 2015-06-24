using Microsoft.AspNet.Builder;
using Microsoft.Framework.Logging;

namespace HelloWeb
{
    public class Startup
    {
        public Startup(ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
        }
        
        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseWelcomePage();
        }
    }
}