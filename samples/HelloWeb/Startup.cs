using Microsoft.AspNet.Builder;

namespace KWebStartup
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            app.UseStaticFiles();
            app.UseWelcomePage();
        }
    }
}