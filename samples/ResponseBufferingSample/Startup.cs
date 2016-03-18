using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ResponseBufferingSample
{
    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseResponseBuffering();
            app.Run(async (context) =>
            {
                // Write some stuff
                context.Response.ContentType = "text/other";
                await context.Response.WriteAsync("Hello World!");

                // ... more work ...

                // Something went wrong and we want to replace the response
                context.Response.StatusCode = 200;
                context.Response.Headers.Clear();
                context.Response.Body.SetLength(0);

                // Try again
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hi Bob!");
            });
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                // .UseIIS() // This repo can no longer reference IIS because IISIntegration depends on it.
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
