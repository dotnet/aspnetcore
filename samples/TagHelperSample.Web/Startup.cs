
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace TagHelperSample.Web
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseServices(services => services.AddMvc());
            app.UseMvc();
        }
    }
}
