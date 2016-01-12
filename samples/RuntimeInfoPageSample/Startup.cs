using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;

namespace RuntimeInfoPageSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRuntimeInfoPage();

            app.Run(context =>
            {
                context.Response.StatusCode = 302;
                context.Response.Headers["Location"] = "/runtimeinfo";

                return Task.FromResult(0);
            });
        }

        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
