using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace WelcomePageSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseWelcomePage();
        }

        public static Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseKestrel()
                    .UseIISIntegration()
                    .UseStartup<Startup>();
                }).Build();

            return host.RunAsync();
        }
    }
}

