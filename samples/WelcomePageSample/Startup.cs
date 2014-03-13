#if NET45
using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;
using Owin;

namespace WelcomePageSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Temporary bridge from katana to Owin
            app.UseBuilder(ConfigurePK);
        }

        private void ConfigurePK(IBuilder builder)
        {
            builder.UseWelcomePage();
        }
    }
}
#endif