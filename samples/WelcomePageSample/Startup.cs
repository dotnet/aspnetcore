using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;

namespace WelcomePageSample
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            app.UseWelcomePage();
        }
    }
}
