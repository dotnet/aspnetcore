using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Interop45.Startup))]
namespace Interop45
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
