using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace WelcomePageSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseWelcomePage();
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultHostingConfiguration(args)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
