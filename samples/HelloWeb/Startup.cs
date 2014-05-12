using Microsoft.AspNet.Builder;

namespace KWebStartup
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            app.UseStaticFiles();
            app.UseWelcomePage();
        }
    }
}