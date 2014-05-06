using Microsoft.AspNet;
using Microsoft.AspNet.Builder;

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
