using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet;

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