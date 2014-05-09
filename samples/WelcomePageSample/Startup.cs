using Microsoft.AspNet;
using Microsoft.AspNet.Builder;

namespace WelcomePageSample
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            app.UseWelcomePage();
        }
    }
}
