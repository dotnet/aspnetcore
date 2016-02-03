using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

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
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
