using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Interop4._5.Startup))]
namespace Interop4._5
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
